using Messenger.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebPush;

namespace Messenger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PushController : ControllerBase
    {
        private readonly IPushSubscriptionService _subscriptionService;
        private readonly WebPushClient _webPushClient;
        private readonly VapidDetails _vapidDetails;
        private readonly ILogger<PushController> _logger;
        private readonly IChatService _chatService;
        private readonly IUserService _userService;

        public PushController(IPushSubscriptionService subscriptionService, IConfiguration configuration,
            ILogger<PushController> logger, IChatService chatService, IUserService userService)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;

            var vapidSection = configuration.GetSection("Vapid");

            _vapidDetails = new VapidDetails(
                subject: vapidSection["Subject"] ?? "mailto:admin@guap.ru",
                publicKey: vapidSection["PublicKey"]!,
                privateKey: vapidSection["PrivateKey"]!
            );

            _webPushClient = new WebPushClient();
            _chatService = chatService;
            _userService = userService;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto subscriptionDto)
        {
            if (subscriptionDto == null || string.IsNullOrEmpty(subscriptionDto.Endpoint))
                return BadRequest(new { error = "Некорректные данные подписки" });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub");

            var user = await _userService.GetUserByExternalIdAsync(userIdClaim);

            // Явно проверяем, что ключи пришли
            if (string.IsNullOrEmpty(subscriptionDto.P256dh) || string.IsNullOrEmpty(subscriptionDto.Auth))
            {
                _logger.LogWarning("Подписка от пользователя {UserId} пришла без p256dh/auth ключей", user!.UserId);
                return BadRequest(new { error = "Отсутствуют ключи шифрования p256dh или auth" });
            }

            var subscription = new Core.Models.PushSubscription
            {
                UserId = user!.UserId,
                Endpoint = subscriptionDto.Endpoint,
                P256dh = subscriptionDto.P256dh,
                Auth = subscriptionDto.Auth,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            };

            await _subscriptionService.RemoveByEndpointAsync(subscriptionDto.Endpoint);

            await _subscriptionService.AddSubscriptionAsync(subscription);

            _logger.LogInformation("Push-подписка успешно сохранена для пользователя {UserId}. P256dh length: {Len}",
                user!.UserId, subscription.P256dh.Length);

            return Ok(new { message = "Подписка успешно сохранена" });
        }

        [HttpDelete("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return BadRequest();

            await _subscriptionService.RemoveByEndpointAsync(endpoint);

            return Ok(new { message = "Подписка удалена" });
        }

        [HttpGet("vapid-public-key")]
        [AllowAnonymous]
        public IActionResult GetVapidPublicKey()
        {
            var publicKey = _vapidDetails.PublicKey;
            return Content(publicKey, "text/plain");
        }        
    }

    public class PushSubscriptionDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }
}

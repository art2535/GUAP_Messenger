using Messenger.Core.Interfaces;
using Messenger.Core.DTOs.Push;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using WebPush;

namespace Messenger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [SwaggerTag("Контроллер для управления push-подписками для веб-уведомлений")]
    public class PushController : ControllerBase
    {
        private readonly IPushSubscriptionService _subscriptionService;
        private readonly VapidDetails _vapidDetails;
        private readonly ILogger<PushController> _logger;
        private readonly IUserService _userService;

        public PushController(IPushSubscriptionService subscriptionService, IConfiguration configuration,
            ILogger<PushController> logger, IUserService userService)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;

            var vapidSection = configuration.GetSection("Vapid");

            _vapidDetails = new VapidDetails(
                subject: vapidSection["Subject"] ?? "mailto:admin@guap.ru",
                publicKey: vapidSection["PublicKey"]!,
                privateKey: vapidSection["PrivateKey"]!
            );

            _userService = userService;
        }

        [HttpPost("subscribe")]
        [SwaggerOperation(
            Summary = "Подписка на push-уведомления",
            Description = "Сохраняет push-подписку пользователя для отправки веб-уведомлений")]
        [SwaggerResponse(StatusCodes.Status200OK, "Подписка успешно сохранена")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные подписки или отсутствуют ключи шифрования")]
        public async Task<IActionResult> SubscribeAsync(
            [SwaggerParameter(Description = "Данные для создания подписки")][FromBody] PushSubscriptionRequest subscriptionDto)
        {
            if (subscriptionDto == null || string.IsNullOrEmpty(subscriptionDto.Endpoint))
                return BadRequest(new { error = "Некорректные данные подписки" });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub");

            var user = await _userService.GetUserByExternalIdAsync(userIdClaim);

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
        [SwaggerOperation(
            Summary = "Отписка от push-уведомлений",
            Description = "Удаляет push-подписку по указанному endpoint")]
        [SwaggerResponse(StatusCodes.Status200OK, "Подписка успешно удалена")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Endpoint не указан")]
        public async Task<IActionResult> UnsubscribeAsync(
            [SwaggerParameter(Description = "Endpoint для удаления подписки")][FromBody] string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return BadRequest();

            await _subscriptionService.RemoveByEndpointAsync(endpoint);

            return Ok(new { message = "Подписка удалена" });
        }

        [HttpGet("vapid-public-key")]
        [SwaggerOperation(
            Summary = "Получить публичный VAPID ключ",
            Description = "Возвращает публичный ключ VAPID, необходимый для подписки на push-уведомления в браузере")]
        [SwaggerResponse(StatusCodes.Status200OK, "Публичный VAPID ключ", typeof(string))]
        public IActionResult GetVapidPublicKey()
        {
            var publicKey = _vapidDetails.PublicKey;
            return Content(publicKey, "text/plain");
        }        
    }
}

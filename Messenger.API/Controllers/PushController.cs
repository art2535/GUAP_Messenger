using Messenger.Core.DTOs.Push;
using Messenger.Core.Interfaces;
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
    [SwaggerTag("Push-уведомления — управление подписками, настройками и отправкой веб-push")]
    public class PushController : ControllerBase
    {
        private readonly IPushSubscriptionService _subscriptionService;
        private readonly VapidDetails _vapidDetails;
        private readonly ILogger<PushController> _logger;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        public PushController(IPushSubscriptionService subscriptionService, IConfiguration configuration,
            ILogger<PushController> logger, IUserService userService, INotificationService notificationService)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;

            var vapidSection = configuration.GetSection("Vapid");

            _vapidDetails = new VapidDetails(vapidSection["Subject"], vapidSection["PublicKey"]!, 
                vapidSection["PrivateKey"]!);

            _userService = userService;
            _notificationService = notificationService;
        }

        [HttpPost("subscribe")]
        [SwaggerOperation(
            Summary = "Подписать пользователя на push-уведомления",
            Description = "Сохраняет push-подписку браузера для последующей отправки веб-уведомлений. " +
                         "Если подписка с таким Endpoint уже существует — она будет заменена.",
            OperationId = "SubscribeToPush")]
        [SwaggerResponse(StatusCodes.Status200OK, "Подписка успешно сохранена")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные подписки или отсутствуют ключи шифрования")]
        [Consumes("application/json")]
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
            Summary = "Отписать пользователя от push-уведомлений",
            Description = "Удаляет push-подписку по указанному endpoint.",
            OperationId = "UnsubscribeFromPush")]
        [SwaggerResponse(StatusCodes.Status200OK, "Подписка успешно удалена")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Endpoint не указан")]
        [Consumes("application/json")]
        public async Task<IActionResult> UnsubscribeAsync(
            [SwaggerParameter(Description = "Endpoint для удаления подписки")][FromBody] string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return BadRequest();

            await _subscriptionService.RemoveByEndpointAsync(endpoint);

            return Ok(new { message = "Подписка удалена" });
        }

        [HttpPost("{notificationId}/read")]
        [SwaggerOperation(
            Summary = "Пометить уведомление как прочитанное",
            Description = "Отмечает конкретное push-уведомление как прочитанное для текущего пользователя.",
            OperationId = "MarkNotificationAsRead")]
        [SwaggerResponse(StatusCodes.Status200OK, "Уведомление успешно помечено как прочитанное")]
        public async Task<IActionResult> MarkAsReadAsync(Guid notificationId)
        {
            await _notificationService.MarkAsReadAsync(notificationId);

            return Ok(new { message = "Уведомление помечено как прочитанное" });
        }

        [HttpGet("settings")]
        [SwaggerOperation(
            Summary = "Получить настройки push-уведомлений текущего пользователя",
            Description = "Возвращает текущие настройки push-уведомлений (включены ли уведомления, о сообщениях, группах и упоминаниях).",
            OperationId = "GetPushSettings")]
        [SwaggerResponse(StatusCodes.Status200OK, "Настройки успешно получены", typeof(PushSubscriptionUpdateRequest))]
        public async Task<IActionResult> GetSettingsAsync(CancellationToken token = default)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var user = await _userService.GetUserByExternalIdAsync(userIdClaim);

            if (user == null || user?.AccountId == null)
                return Ok(new PushSubscriptionUpdateRequest());

            var settings = await _subscriptionService.GetPushSettingsAsync(user.AccountId, token);

            if (settings == null)
            {
                return Ok(new PushSubscriptionUpdateRequest
                {
                    PushEnabled = true,
                    NotifyMessages = true,
                    NotifyGroupChats = true,
                    NotifyMentions = true
                });
            }

            return Ok(new PushSubscriptionUpdateRequest
            {
                PushEnabled = settings.PushEnabled,
                NotifyMessages = settings.NotifyMessages,
                NotifyGroupChats = settings.NotifyGroupChats,
                NotifyMentions = settings.NotifyMentions
            });
        }

        [HttpPost("settings")]
        [SwaggerOperation(
            Summary = "Сохранить настройки push-уведомлений",
            Description = "Обновляет настройки push-уведомлений пользователя (включение/отключение всех уведомлений, сообщений, групп и упоминаний).",
            OperationId = "SavePushSettings")]
        [SwaggerResponse(StatusCodes.Status200OK, "Настройки успешно сохранены")]
        public async Task<IActionResult> SaveSettingsAsync([FromBody] PushSubscriptionUpdateRequest request,
            CancellationToken token = default)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var user = await _userService.GetUserByExternalIdAsync(userIdClaim);

            await _subscriptionService.SavePushSettingsAsync(user.UserId, user.AccountId, request, token);

            _logger.LogInformation("Настройки push-уведомлений обновлены для пользователя {UserId}", user.UserId);

            return Ok(new { message = "Настройки push-уведомлений успешно сохранены" });
        }

        [HttpPost("send")]
        [SwaggerOperation(
            Summary = "Отправить push-уведомления участникам чата",
            Description = "Отправляет веб-push уведомления всем участникам чата (кроме отправителя), " +
                         "у которых включены соответствующие настройки. Вызывается после отправки сообщения.",
            OperationId = "SendPushNotification")]
        [SwaggerResponse(StatusCodes.Status200OK, "Push-уведомления успешно обработаны")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные запроса")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        public async Task<IActionResult> SendPushNotificationAsync(
            [FromBody][SwaggerParameter(Description = "Данные для отправки уведомления")] SendPushNotificationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null || request.ChatId == Guid.Empty)
                return BadRequest(new { error = "Некорректные данные запроса" });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub");

            var sender = await _userService.GetUserByExternalIdAsync(userIdClaim);
            if (sender == null)
                return Unauthorized();

            try
            {
                await _subscriptionService.SendPushToOfflineUsersAsync(request.ChatId, sender.UserId,
                    request.SenderName ?? "Пользователь", request.MessageText, request.HasAttachments,
                    request.IsMention, cancellationToken);

                return Ok(new { message = "Push-уведомления обработаны" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке push-уведомлений для чата {ChatId}", request.ChatId);
                return StatusCode(500, new { error = "Внутренняя ошибка сервера при отправке push" });
            }
        }

        [HttpGet("vapid-public-key")]
        [SwaggerOperation(
            Summary = "Получить публичный VAPID-ключ",
            Description = "Возвращает публичный VAPID-ключ, необходимый на клиентской стороне для создания push-подписки через Service Worker.",
            OperationId = "GetVapidPublicKey")]
        [SwaggerResponse(StatusCodes.Status200OK, "Публичный VAPID-ключ возвращён", typeof(string))]
        public IActionResult GetVapidPublicKey()
        {
            var publicKey = _vapidDetails.PublicKey;
            return Content(publicKey, "text/plain");
        }        
    }
}

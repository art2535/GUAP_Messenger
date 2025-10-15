using Messenger.Core.DTOs.Notifications;
using Messenger.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Messenger.API.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Контроллер для управления уведомлениями")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Создание нового уведомления",
            Description = "Создает уведомление для конкретного пользователя по его идентификатору.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> CreateNotificationAsync([FromBody] CreateNotificationRequest request, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(request.UserId, request.Text, cancellationToken);

                return Ok(new 
                { 
                    IsSuccess = true, 
                    Message = "Уведомление успешно создано" 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    IsSuccess = false, 
                    Error = ex.Message 
                });
            }
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Получение уведомлений текущего пользователя",
            Description = "Возвращает список всех уведомлений, связанных с авторизованным пользователем.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetNotificationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var notifications = await _notificationService.GetNotificationsAsync(userId, cancellationToken);

                return Ok(new 
                { 
                    IsSuccess = true, 
                    Data = notifications 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    IsSuccess = false, 
                    Error = ex.Message 
                });
            }
        }
    }
}

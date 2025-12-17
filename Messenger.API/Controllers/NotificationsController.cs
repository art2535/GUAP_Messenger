using Messenger.API.Responses;
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
    [Produces("application/json")]
    [Consumes("application/json")]
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
            Summary = "Создать новое уведомление",
            Description = "Создаёт уведомление для указанного пользователя. " +
                          "Обычно вызывается внутренними сервисами (например, при новом сообщении, добавлении в чат и т.д.).")]
        [SwaggerResponse(StatusCodes.Status200OK, "Уведомление успешно создано", typeof(CreateNotificationSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные запроса", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Пользователь с указанным ID не найден", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> CreateNotificationAsync(
            [FromBody] [SwaggerParameter(Description = "Данные для создания уведомления", Required = true)] 
            CreateNotificationRequest request, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(request.UserId, request.Text, cancellationToken);

                return Ok(new CreateNotificationSuccessResponse
                { 
                    IsSuccess = true, 
                    Message = "Уведомление успешно создано" 
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = $"Пользователь с ID {request.UserId} не найден"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Получить уведомления текущего пользователя",
            Description = "Возвращает список всех активных и непрочитанных уведомлений авторизованного пользователя.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Уведомления успешно получены", typeof(GetNotificationsSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetNotificationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var notifications = await _notificationService.GetNotificationsAsync(userId, cancellationToken);

                return Ok(new GetNotificationsSuccessResponse
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

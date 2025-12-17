using Messenger.API.Responses;
using Messenger.Core.DTOs.UserStatuses;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;
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
    [SwaggerTag("Контроллер для управления статусами пользователей")]
    public class UserStatusesController : ControllerBase
    {
        private readonly IUserStatusService _userStatusService;

        public UserStatusesController(IUserStatusService userStatusService)
        {
            _userStatusService = userStatusService;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Получить статус текущего пользователя",
            Description = "Возвращает текущий статус авторизованного пользователя: онлайн/офлайн и время последней активности.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Статус успешно получен", typeof(GetUserStatusSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Статус пользователя не найден", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetUserStatusesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var status = await _userStatusService.GetStatusByUserIdAsync(userId, cancellationToken);

                if (status == null)
                {
                    return NotFound(new
                    {
                        IsSuccess = false,
                        Error = "Статус пользователя не найден"
                    });
                }

                return Ok(new GetUserStatusSuccessResponse
                {
                    IsSuccess = true,
                    Data = status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [HttpPut]
        [SwaggerOperation(
            Summary = "Обновить статус текущего пользователя",
            Description = "Обновляет статус онлайн/офлайн для авторизованного пользователя. " +
                          "Поле LastActivity автоматически устанавливается на текущее время сервера (UTC).")]
        [SwaggerResponse(StatusCodes.Status200OK, "Статус успешно обновлён", typeof(UpdateUserStatusSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные запроса", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> UpdateStatusAsync(
            [FromBody] [SwaggerParameter(Description = "Новый статус пользователя", Required = true)] UpdateStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userStatus = new UserStatus
                {
                    UserId = userId,
                    Online = request.Online,
                    LastActivity = DateTime.UtcNow
                };

                await _userStatusService.UpdateStatusAsync(userStatus, cancellationToken);

                return Ok(new UpdateUserStatusSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Статус пользователя обновлен"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }
    }
}

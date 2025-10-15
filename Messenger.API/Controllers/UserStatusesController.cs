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
            Summary = "Получение статуса текущего пользователя",
            Description = "Возвращает текущий статус пользователя (онлайн/офлайн) и дату последней активности. " +
                          "Требуется авторизация по JWT-токену.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
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

                return Ok(new
                {
                    IsSuccess = true,
                    Data = status
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

        [HttpPut]
        [SwaggerOperation(
            Summary = "Обновление статуса пользователя",
            Description = "Позволяет обновить текущий статус пользователя (например, установить 'онлайн' или 'офлайн'). " +
                          "Поле 'LastActivity' обновляется автоматически на сервере. " +
                          "Требуется авторизация по JWT-токену.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> UpdateStatusAsync([FromBody] UpdateStatusRequest request,
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

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Статус пользователя обновлен"
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

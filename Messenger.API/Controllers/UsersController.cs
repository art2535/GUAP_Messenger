using Messenger.Core.Interfaces;
using Messenger.Core.DTOs.Users;
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
    [SwaggerTag("Контроллер для управления личным кабинетом пользователей")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Фильтрация пользователей",
            Description = "Отображает список пользователей при вводе")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> Search([FromQuery] string query, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Ok(new { isSuccess = true, data = Array.Empty<object>() });

            var result = await _userService.SearchUsersAsync(query.Trim(), token);

            var data = result.Select(u => new
            {
                id = u.Id,
                name = u.Name,
                avatar = u.Avatar ?? "https://static.photos/people/200x200/default"
            });

            return Ok(new
            {
                IsSuccess = true,
                Data = data
            });
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Получить список всех пользователей",
            Description = "Возвращает список всех зарегистрированных пользователей")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetAllUsersAsync(CancellationToken token = default)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(token);

                return Ok(new
                {
                    IsSuccess = true,
                    Data = users
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Error = ex.Message,
                });
            }
        }

        [HttpGet("roles")]
        [SwaggerOperation(
            Summary = "Получить список всех ролей",
            Description = "Возвращает список всех доступных ролей в системе")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetAllRolesAsync(CancellationToken token = default)
        {
            try
            {
                var roles = await _userService.GetRolesAsync(token);

                return Ok(new
                {
                    IsSuccess = true,
                    Data = roles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Error = ex.Message,
                });
            }
        }

        [HttpGet("info")]
        [SwaggerOperation(
            Summary = "Получить пользователя по ID",
            Description = "Возвращает информацию о пользователе по его идентификатору")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetUserByIdAsync(CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var user = await _userService.GetUserByIdAsync(userId, token);

                return Ok(new
                {
                    IsSuccess = true,
                    Data = user
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

        [HttpPut("update-profile")]
        [SwaggerOperation(
            Summary = "Обновить профиль текущего пользователя",
            Description = "Позволяет обновить личные данные и тему аккаунта текущего пользователя")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> UpdateUserProfileByIdAsync([FromBody] UpdateUserProfileRequest request,
            [FromQuery] string? avatarUrl = null, CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                await _userService.UpdateProfileAsync(userId, request, avatarUrl, token);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Пользователь успешно обновлен"
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

        [HttpPost("block/{blockedUserId}")]
        [SwaggerOperation(
            Summary = "Заблокировать пользователя",
            Description = "Добавляет пользователя в список заблокированных текущего пользователя")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> BlockUserAsync(Guid blockedUserId, CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _userService.BlockUserAsync(userId, blockedUserId, token);

                return Ok(new 
                { 
                    Success = true, 
                    Message = "Пользователь успешно заблокирован" 
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

        [HttpPost("unblock/{blockedUserId}")]
        [SwaggerOperation(
            Summary = "Разблокировать пользователя",
            Description = "Удаляет пользователя из списка заблокированных текущего пользователя"
        )]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> UnblockUserAsync(Guid blockedUserId, CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _userService.UnblockUserAsync(userId, blockedUserId, token);

                return Ok(new 
                { 
                    Success = true, 
                    Message = "Пользователь успешно разблокирован" 
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

        [HttpPost("change-password")]
        [SwaggerOperation(
            Summary = "Сменить пароль пользователя",
            Description = "Позволяет пользователю сменить текущий пароль, указав старый и новый")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request,
            CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword, token);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Пароль успешно обновлен"
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

        [HttpPost("assign-role/{roleId}")]
        [SwaggerOperation(
            Summary = "Назначить роль пользователю",
            Description = "Назначает текущему пользователю указанную роль по её идентификатору")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> AssignRoleAsync(Guid roleId, CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _userService.AssignRoleAsync(userId, roleId, token);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Роль успешно определена"
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

        [HttpDelete("delete-account")]
        [SwaggerOperation(
            Summary = "Удалить аккаунт пользователя",
            Description = "Удаляет аккаунт текущего пользователя из системы")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> DeleteAccountAsync(CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _userService.DeleteAccountAsync(userId, token);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Аккаунт успешно удален"
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

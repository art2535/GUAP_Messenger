using Messenger.API.Responses;
using Messenger.Core.DTOs.Users;
using Messenger.Core.Hubs;
using Messenger.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<ChatHub> _hubContext;

        public UsersController(IUserService userService, IHubContext<ChatHub> hubContext)
        {
            _userService = userService;
            _hubContext = hubContext;
        }

        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Поиск пользователей",
            Description = "Возвращает список пользователей, соответствующих поисковому запросу (минимум 2 символа). Используется для автодополнения при добавлении в чат или поиске контактов.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Результат поиска успешно получен", typeof(SearchUsersSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> Search(
            [FromQuery] [SwaggerParameter(Description = "Поисковый запрос (минимум 2 символа)")] string query, 
            CancellationToken token = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Ok(new SearchUsersSuccessResponse
                    {
                        IsSuccess = true,
                        Data = Array.Empty<object>()
                    });
                }

                var result = await _userService.SearchUsersAsync(query.Trim(), token);

                var data = result.Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    avatar = u.Avatar ?? "https://static.photos/people/200x200/default"
                });

                return Ok(new SearchUsersSuccessResponse
                {
                    IsSuccess = true,
                    Data = data
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

        [HttpGet]
        [SwaggerOperation(
            Summary = "Получить список всех пользователей",
            Description = "Возвращает полный список зарегистрированных пользователей системы.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Список пользователей получен", typeof(GetAllUsersSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAllUsersAsync(CancellationToken token = default)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(token);

                return Ok(new GetAllUsersSuccessResponse
                {
                    IsSuccess = true,
                    Data = users
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message,
                });
            }
        }

        [HttpGet("roles")]
        [SwaggerOperation(
            Summary = "Получить список всех ролей",
            Description = "Возвращает список всех доступных ролей в системе (например, User, Admin).")]
        [SwaggerResponse(StatusCodes.Status200OK, "Список ролей получен", typeof(GetRolesSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetAllRolesAsync(CancellationToken token = default)
        {
            try
            {
                var roles = await _userService.GetRolesAsync(token);

                return Ok(new GetRolesSuccessResponse
                {
                    IsSuccess = true,
                    Data = roles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message,
                });
            }
        }

        [HttpGet("{userId}/name")]
        [SwaggerOperation(
            Summary = "Получить отображаемое имя пользователя по ID",
            Description = "Возвращает полное имя (Имя Фамилия) пользователя по его GUID.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Имя пользователя получено", typeof(GetUserNameSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Пользователь не найден", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetUserDisplayName(
            [SwaggerParameter(Description = "Идентификатор пользователя (GUID)")] Guid userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new ErrorResponse
                    {
                        IsSuccess = false,
                        Error = "Пользователь не найден"
                    });
                }

                var name = $"{user.FirstName} {user.LastName}".Trim() ?? "Удалённый пользователь";

                return Ok(new GetUserNameSuccessResponse 
                { 
                    IsSuccess = true, 
                    Data = name 
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

        [HttpGet("info")]
        [SwaggerOperation(
            Summary = "Получить информацию о текущем пользователе",
            Description = "Возвращает личные данные авторизованного пользователя: имя, логин, телефон, аватар и тему.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Информация о пользователе получена", typeof(GetCurrentUserSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Пользователь не найден", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetUserByIdAsync(CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var user = await _userService.GetUserByIdAsync(userId, token);

                if (user == null)
                { 
                    return NotFound(); 
                }

                var response = new
                {
                    UserId = user.UserId,
                    LastName = user.LastName ?? "",
                    FirstName = user.FirstName ?? "",
                    MiddleName = user.MiddleName,
                    Login = user.Login ?? "",
                    Phone = user.Phone,
                    Account = user.Account != null ? new
                    {
                        Avatar = user.Account.Avatar,
                        Theme = user.Account.Theme ?? "light"
                    } : null
                };

                return Ok(new GetCurrentUserSuccessResponse
                {
                    IsSuccess = true,
                    Data = response
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

        [HttpPut("update-profile")]
        [SwaggerOperation(
            Summary = "Обновить профиль текущего пользователя",
            Description = "Обновляет личные данные пользователя (имя, телефон и т.д.) и тему интерфейса. Аватар можно передать отдельно через upload-avatar.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Профиль успешно обновлён", typeof(UpdateProfileSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> UpdateUserProfileByIdAsync(
            [FromBody] [SwaggerParameter(Description = "Новые данные профиля", Required = true)] UpdateUserProfileRequest request,
            [FromQuery] [SwaggerParameter(Description = "Опциональный URL аватара")] string? avatarUrl = null, 
            CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                await _userService.UpdateProfileAsync(userId, request, avatarUrl, token);

                var updatedUser = await _userService.GetUserByIdAsync(userId, token);
                var newDisplayName = $"{updatedUser.FirstName} {updatedUser.LastName}".Trim();
                var currentAvatar = updatedUser.Account?.Avatar;

                await _hubContext.Clients.All.SendAsync("ProfileUpdated", new
                {
                    userId = userId.ToString(),
                    avatarUrl = currentAvatar,
                    displayName = newDisplayName
                }, token);

                return Ok(new UpdateProfileSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Пользователь успешно обновлен"
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

        [HttpGet("blocked")]
        [SwaggerOperation(
            Summary = "Получить список заблокированных пользователей",
            Description = "Возвращает список пользователей, которых текущий пользователь добавил в чёрный список.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Список заблокированных получен", typeof(GetBlockedUsersSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetBlockedUsersAsync(CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var blockedUsers = await _userService.GetBlockedUsersAsync(userId, token);

                var result = blockedUsers.Select(u => new
                {
                    id = u.UserId.ToString(),
                    name = $"{u.LastName} {u.FirstName}".Trim(),
                    login = u.Login,
                    avatar = u.Account?.Avatar ?? "/images/default-avatar.png"
                });

                return Ok(new GetBlockedUsersSuccessResponse
                { 
                    IsSuccess = true, 
                    Data = result 
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

        [HttpPost("block/{blockedUserId}")]
        [SwaggerOperation(
            Summary = "Заблокировать пользователя",
            Description = "Добавляет указанного пользователя в чёрный список текущего пользователя. Уведомления отправляются через SignalR.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Пользователь заблокирован", typeof(BlockUserSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Нельзя заблокировать себя", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Пользователь не найден", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> BlockUserAsync(
            [SwaggerParameter(Description = "Идентификатор блокируемого пользователя")] Guid blockedUserId, 
            CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                if (userId == blockedUserId)
                {
                    return BadRequest(new ErrorResponse
                    {
                        IsSuccess = false,
                        Error = "Нельзя заблокировать себя"
                    });
                }

                await _userService.BlockUserAsync(userId, blockedUserId, token);

                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("UserBlocked", blockedUserId.ToString(), token);

                await _hubContext.Clients.User(blockedUserId.ToString())
                    .SendAsync("UserBlockedMe", userId.ToString(), token);

                return Ok(new BlockUserSuccessResponse
                { 
                    IsSuccess = true, 
                    Message = "Пользователь успешно заблокирован" 
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

        [HttpPost("upload-avatar")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [SwaggerOperation(
            Summary = "Загрузить аватар пользователя",
            Description = "Загружает изображение аватара для текущего пользователя (макс. 2 МБ). Возвращает URL загруженного аватара.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Аватар успешно загружен", typeof(UploadAvatarSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Файл не выбран или слишком большой", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> UploadAvatar(
            [FromForm] [SwaggerParameter(Description = "Файл аватара (изображение)", Required = true)] IFormFile avatarFile, 
            CancellationToken token = default)
        {
            if (avatarFile == null || avatarFile.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Файл не выбран"
                });
            }

            if (avatarFile.Length > 2 * 1024 * 1024)
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Файл не должен превышать 2 МБ"
                });
            }

            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var avatarUrl = await _userService.UploadAvatarAsync(userId, avatarFile, token);

                await _hubContext.Clients.All.SendAsync("AvatarUpdated", new
                {
                    userId = userId.ToString(),
                    avatarUrl = avatarUrl
                }, token);

                return Ok(new UploadAvatarSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Аватар успешно загружен",
                    Data = new { avatarUrl }
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
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse 
                { 
                    IsSuccess = false, 
                    Error = ex.Message 
                });
            }
        }

        [HttpPost("unblock/{blockedUserId}")]
        [SwaggerOperation(
            Summary = "Разблокировать пользователя",
            Description = "Удаляет пользователя из чёрного списка текущего пользователя. Уведомления отправляются через SignalR.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Пользователь разблокирован", typeof(UnblockUserSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> UnblockUserAsync(
            [SwaggerParameter(Description = "Идентификатор разблокируемого пользователя")] Guid blockedUserId, 
            CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _userService.UnblockUserAsync(userId, blockedUserId, token);

                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("UserUnblocked", blockedUserId.ToString());

                await _hubContext.Clients.User(blockedUserId.ToString())
                    .SendAsync("UserUnblockedMe", userId.ToString());

                return Ok(new UnblockUserSuccessResponse
                { 
                    IsSuccess = true, 
                    Message = "Пользователь успешно разблокирован" 
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

        [HttpPost("change-password")]
        [SwaggerOperation(
            Summary = "Сменить пароль",
            Description = "Меняет пароль текущего пользователя после проверки старого пароля.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Пароль успешно изменён", typeof(ChangePasswordSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Неверный старый пароль", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> ChangePasswordAsync(
            [FromBody] [SwaggerParameter(Description = "Старый и новый пароль", Required = true)] ChangePasswordRequest request,
            CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword, token);

                return Ok(new ChangePasswordSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Пароль успешно обновлен"
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

        [HttpPost("assign-role/{roleId}")]
        [SwaggerOperation(
            Summary = "Назначить роль текущему пользователю",
            Description = "Назначает указанную роль авторизованному пользователю (для администраторов).")]
        [SwaggerResponse(StatusCodes.Status200OK, "Роль успешно назначена", typeof(AssignRoleSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> AssignRoleAsync(
            [SwaggerParameter(Description = "Идентификатор роли")] Guid roleId, CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _userService.AssignRoleAsync(userId, roleId, token);

                return Ok(new AssignRoleSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Роль успешно определена"
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

        [HttpGet("is-blocked-by/{userId}")]
        [SwaggerOperation(
            Summary = "Проверить, заблокировал ли пользователь текущего",
            Description = "Возвращает true, если указанный пользователь добавил текущего в чёрный список.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Результат проверки получен", typeof(IsBlockedByResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> IsBlockedByUser(
            [SwaggerParameter(Description = "Идентификатор пользователя, которого проверяем")] Guid userId, 
            CancellationToken token = default)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var isBlocked = await _userService.IsBlockedByAsync(userId, currentUserId, token);

                return Ok(new IsBlockedByResponse 
                { 
                    IsBlocked = isBlocked 
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

        [HttpDelete("delete-account")]
        [SwaggerOperation(
            Summary = "Удалить аккаунт",
            Description = "Безвозвратно удаляет аккаунт текущего пользователя из системы.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Аккаунт успешно удалён", typeof(DeleteAccountSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteAccountAsync(CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _userService.DeleteAccountAsync(userId, token);

                return Ok(new DeleteAccountSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Аккаунт успешно удален"
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

        [HttpDelete("delete-avatar")]
        [SwaggerOperation(
            Summary = "Удалить аватар",
            Description = "Удаляет текущий аватар пользователя и устанавливает стандартный. Уведомление рассылается через SignalR.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Аватар успешно удалён", typeof(DeleteAvatarSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteAvatar(CancellationToken token = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                await _userService.DeleteAvatarAsync(userId, token);

                await _hubContext.Clients.All.SendAsync("AvatarUpdated", new
                {
                    userId = userId.ToString(),
                    avatarUrl = (string?)null
                });

                return Ok(new DeleteAvatarSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Аватар успешно удалён"
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

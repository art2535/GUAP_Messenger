using Messenger.Core.DTOs.Auth;
using Messenger.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Messenger.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Контроллер для авторизации и регистрации пользователей")]
    public class AuthorizationController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthorizationController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        [SwaggerOperation(
            Summary = "Регистрация пользователя в системе",
            Description = "Регистрирует нового пользователя по его данным")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> RegisterUserAsync([FromBody] RegisterRequest registerRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (user, token, role) = await _userService.RegisterAsync(registerRequest.Email, registerRequest.Password,
                    registerRequest.FirstName, registerRequest.MiddleName, registerRequest.LastName, registerRequest.Phone,
                    registerRequest.BirthDate, null, cancellationToken);

                if (user != null && token == null)
                {
                    throw new Exception($"Пользователь с email {user.Login} уже существует");
                }

                return Ok(new
                {
                    IsSuccess = true,
                    User = user,
                    Role = role,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Авторизация и аутентификация пользователя",
            Description = "Авторизация и аутентификация пользователя с выдачей JWT-токена")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> LoginUserAsync([FromBody] LoginRequest loginRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (token, userId, role) = 
                    await _userService.LoginAsync(loginRequest.Login, loginRequest.Password, cancellationToken);

                var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
                var fullName = string.Join(" ", new[] { user?.FirstName, user?.LastName }
                    .Where(s => !string.IsNullOrEmpty(s)));

                return Ok(new
                {
                    IsSuccess = true,
                    UserId = userId,
                    Role = role,
                    Token = token,
                    FullName = fullName
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("logout")]
        [SwaggerOperation(
            Summary = "Выход пользователя из системы",
            Description = "Инвалидация JWT-токена")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        public IActionResult LogoutUserAsync()
        {
            return Ok(new
            {
                IsSuccess = true,
                Message = "Вы успешно вышли из системы"
            });
        }
    }
}

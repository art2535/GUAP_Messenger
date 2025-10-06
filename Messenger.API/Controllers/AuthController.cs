using Messenger.API.DTOs;
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
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        [SwaggerOperation(
            Summary = "Регистрация пользователя в системе",
            Description = "Регистрирует нового пользователя по его данным")]
        public async Task<IActionResult> RegisterUserAsync([FromBody] RegisterRequest registerRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (user, token) = await _userService.RegisterAsync(registerRequest.Email, registerRequest.Password,
                    registerRequest.FirstName, registerRequest.MiddleName, registerRequest.LastName, registerRequest.Phone,
                    registerRequest.BirthDate, null, cancellationToken);

                if (user != null && token == null)
                {
                    throw new Exception($"Пользователь с email {user.Login} уже существует");
                }

                Response.Cookies.Append("JWT_SECRET", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(2)
                });

                return Ok(new
                {
                    IsSuccess = true,
                    User = user,
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
        public async Task<IActionResult> LoginUserAsync([FromBody] LoginRequest loginRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _userService.LoginAsync(loginRequest.Login, loginRequest.Password, cancellationToken);

                Response.Cookies.Append("JWT_SECRET", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(2)
                });

                return Ok(new
                {
                    IsSuccess = true,
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

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("logout")]
        [SwaggerOperation(
            Summary = "Выход пользователя из системы",
            Description = "Инвалидация JWT-токена")]
        public async Task<IActionResult> LogoutUserAsync([FromBody] LogoutRequest logoutRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var token = Request.Cookies["JWT_SECRET"];

                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized("JWT токен не найден");
                }

                var logoutUser = await _userService.LogoutAsync(logoutRequest.Login, logoutRequest.Password,
                    cancellationToken)
                    ?? throw new Exception($"Пользователя с email {logoutRequest.Login} не существует");

                Response.Cookies.Delete("JWT_SECRET", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Вы успешно вышли из системы"
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
    }
}

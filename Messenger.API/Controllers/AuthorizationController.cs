using Messenger.API.Responses;
using Messenger.Core.DTOs.Auth;
using Messenger.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;

namespace Messenger.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [SwaggerTag("Контроллер для авторизации и регистрации пользователей")]
    public class AuthorizationController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthorizationController(IUserService userService, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _userService = userService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        
        [HttpPost("register")]
        [SwaggerOperation(
            Summary = "Регистрация нового пользователя",
            Description = "Регистрирует нового пользователя и возвращает JWT-токен для аутентификации")]
        [SwaggerResponse(StatusCodes.Status200OK, "Регистрация прошла успешно", typeof(RegisterSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации или пользователь уже существует", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")]
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
                    return BadRequest(new ErrorResponse
                    {
                        IsSuccess = false,
                        Error = $"Пользователь с email {user.Login} уже существует"
                    });
                }

                return Ok(new RegisterSuccessResponse
                {
                    IsSuccess = true,
                    User = user,
                    Role = role,
                    Token = token
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

        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Аутентификация пользователя",
            Description = "Выполняет вход пользователя и возвращает JWT-токен, идентификатор, роль и полное имя")]
        [SwaggerResponse(StatusCodes.Status200OK, "Вход выполнен успешно", typeof(LoginSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Неверные учетные данные", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")]
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

                return Ok(new LoginSuccessResponse
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
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [HttpPost("refresh")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var client = _httpClientFactory.CreateClient();

            var formData = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", request.RefreshToken },
                { "client_id", _configuration["AzureAd:ClientId"] ?? "messager" },
                { "client_secret", _configuration["AzureAd:ClientSecret"] }
            };

            var content = new FormUrlEncodedContent(formData);

            var keycloakTokenUrl = "https://sso.guap.ru/realms/master/protocol/openid-connect/token";

            var response = await client.PostAsync(keycloakTokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return BadRequest(new { error = "Failed to refresh token", details = errorContent });
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            return Ok(new
            {
                access_token = tokenResponse["access_token"],
                id_token = tokenResponse.GetValueOrDefault("id_token"),
                refresh_token = tokenResponse.GetValueOrDefault("refresh_token"),
                expires_in = tokenResponse["expires_in"]
            });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("logout")]
        [SwaggerOperation(
            Summary = "Выход из системы",
            Description = "Логический выход пользователя. Токен удаляется на стороне клиента.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Выход выполнен успешно", typeof(LogoutSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Требуется аутентификация")]
        public IActionResult LogoutUserAsync()
        {
            return Ok(new LogoutSuccessResponse
            {
                IsSuccess = true,
                Message = "Вы успешно вышли из системы"
            });
        }
    }
}

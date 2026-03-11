using Messenger.API.Responses;
using Messenger.Core.DTOs.Auth;
using Messenger.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
        private readonly ILoginService _loginService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthorizationController(IUserService userService, IConfiguration configuration, 
            IHttpClientFactory httpClientFactory, ILoginService loginService)
        {
            _userService = userService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _loginService = loginService;
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

        [HttpPost("external/callback")]
        [SwaggerOperation(
            Summary = "Аутентификация пользователя через ЕТА",
            Description = "Выполняет запись входа в базу данных")]
        [SwaggerResponse(StatusCodes.Status200OK, "Вход выполнен успешно", typeof(LoginSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Неверные учетные данные", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalCallbackAsync([FromBody] ExternalLoginRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userService.GetUserByExternalIdAsync(request.ExternalId);

                if (user == null)
                {
                    user = await _userService.RegisterExternalUserAsync(request.ExternalId, request.Email, request.FirstName,
                        request.LastName, request.MiddleName);
                }

                var fakePassword = $"external_{request.ExternalId.Substring(0, 8)}";

                var (token, userId, role) =
                    await _userService.LoginAsync(user.Login, request.FakePasswordForInternalUse, cancellationToken);

                var fullName = string.Join(" ", new[] { user.FirstName, user.LastName }
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
                Console.WriteLine($"ОШИБКА: {ex.Message}");
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }
    }
}

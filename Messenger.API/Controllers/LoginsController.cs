using Messenger.API.Responses;
using Messenger.Core.DTOs.Logins;
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
    [SwaggerTag("Контроллер для управления входами в мессенджер")]
    public class LoginsController : ControllerBase
    {
        private readonly ILoginService _loginService;

        public LoginsController(ILoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Получение истории входов текущего пользователя",
            Description = "Возвращает список всех сессий (входов) авторизованного пользователя. " +
                          "Включает информацию о токене, IP-адресе, времени входа и статусе активности.")]
        [SwaggerResponse(StatusCodes.Status200OK, "История входов успешно получена", typeof(GetLoginsSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetLoginsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var logins = await _loginService.GetLoginsByUserIdAsync(userId, cancellationToken);

                return Ok(new GetLoginsSuccessResponse
                {
                    IsSuccess = true,
                    Data = logins
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

        [HttpPost]
        [SwaggerOperation(
            Summary = "Регистрация нового входа в систему",
            Description = "Создаёт запись о новом входе пользователя (новая сессия). " +
                          "Вызывается после успешной аутентификации. " +
                          "Требуется действительный JWT-токен.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Запись о входе успешно создана", typeof(CreateLoginSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные запроса", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> LoginAsync(
            [FromBody] [SwaggerParameter(Description = "Данные новой сессии входа", Required = true)] CreateLoginRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var login = new Login
                {
                    LoginId = Guid.NewGuid(),
                    UserId = userId,
                    Token = request.Token,
                    IpAddress = request.IpAddress,
                    LoginTime = DateTime.Now,
                    Active = true
                };

                await _loginService.AddLoginAsync(login, cancellationToken);

                return Ok(new CreateLoginSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Вход успешно добавлен"
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

        [HttpPatch]
        [SwaggerOperation(
            Summary = "Регистрация выхода из системы",
            Description = "Помечает текущую активную сессию пользователя как неактивную (выход). " +
                          "Вызывается при логауте. Устанавливает время выхода и деактивирует сессию.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Выход успешно зарегистрирован", typeof(LogoutSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Активная сессия не найдена", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var logins = await _loginService.GetLoginsByUserIdAsync(userId, cancellationToken);

                var userLogout = logins.FirstOrDefault(l => l.UserId == userId && l.Active == true);
                if (userLogout != null)
                {
                    userLogout.Active = false;
                    userLogout.LogoutTime = DateTime.Now;
                    await _loginService.UpdateLoginAsync(userLogout, cancellationToken);
                }

                return Ok(new LogoutSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Выход успешно обновлен"
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

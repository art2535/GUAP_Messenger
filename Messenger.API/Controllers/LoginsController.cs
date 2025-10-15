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
            Description = "Возвращает список всех сессий (входов) пользователя, авторизованного по JWT-токену. " +
                          "Каждая запись включает информацию о токене, IP-адресе и времени входа.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetLoginsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var logins = await _loginService.GetLoginsByUserIdAsync(userId, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Data = logins
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

        [HttpPost]
        [SwaggerOperation(
            Summary = "Добавление новой записи входа (логина)",
            Description = "Создаёт новую запись о входе пользователя в систему. " +
                          "Требуется JWT-аутентификация. " +
                          "В теле запроса передаются данные токена и IP-адрес пользователя.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> LoginAsync([FromBody] CreateLoginRequest request,
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
                    LoginTime = DateTime.UtcNow,
                    Active = true
                };

                await _loginService.AddLoginAsync(login, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Вход успешно добавлен"
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

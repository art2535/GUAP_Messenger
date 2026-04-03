using Messenger.API.Responses;
using Messenger.Core.DTOs.Auth;
using Messenger.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Messenger.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [SwaggerTag("Контроллер для авторизации пользователей через ЕТА ГУАП")]
    public class AuthorizationController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthorizationController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("external/callback")]
        [SwaggerOperation(
            Summary = "Аутентификация пользователя через ЕТА",
            Description = "Выполняет запись входа в базу данных")]
        [SwaggerResponse(StatusCodes.Status200OK, "Вход выполнен успешно", typeof(LoginEtaResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Неверные учетные данные", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")]
        public async Task<IActionResult> ExternalCallbackAsync([FromBody] ExternalLoginRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userService.GetUserByExternalIdAsync(request.ExternalId);

                user ??= await _userService.RegisterExternalUserAsync(request.ExternalId, request.Email, request.FirstName,
                        request.LastName, request.MiddleName);

                var fullName = string.Join(" ", new[] { user.FirstName, user.LastName }
                    .Where(s => !string.IsNullOrEmpty(s)));

                return Ok(new LoginEtaResponse
                {
                    IsSuccess = true,
                    UserId = user!.UserId,
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
    }
}

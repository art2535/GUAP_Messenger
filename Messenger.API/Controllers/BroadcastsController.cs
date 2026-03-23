using Messenger.API.Responses;
using Messenger.Infrastructure.Services;
using Messenger.Core.DTOs.Broadcasts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using Messenger.Core.Interfaces;

namespace Messenger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [SwaggerTag("Контроллер для управления рассылками сообщений")]
    public class BroadcastsController : ControllerBase
    {
        private readonly BroadcastService _service;
        private readonly IUserService _userService;

        public BroadcastsController(BroadcastService service, IUserService userService)
        {
            _service = service;
            _userService = userService;
        }

        private async Task<Guid> GetCurrentUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var extrnalId))
                throw new UnauthorizedAccessException("Не удалось определить пользователя");

            var user = await _userService.GetUserByExternalIdAsync(id);

            return user!.UserId;
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Создать новую рассылку",
            Description = "Позволяет создать массовую рассылку сообщений пользователям.")]
        [SwaggerResponse(StatusCodes.Status201Created, "Рассылка успешно создана", typeof(BroadcastCreatedResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Неверные входные данные", typeof(string))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> CreateBroadcastAsync(
            [SwaggerParameter(Description = "Данные для создания рассылки")][FromBody] CreateBroadcastRequest? request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Тело запроса отсутствует");

                var senderId = await GetCurrentUserId();
                var response = await _service.CreateBroadcastAsync(request, senderId);

                return Created($"/api/broadcasts/{response.BroadcastId}", response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Внутренняя ошибка при создании рассылки"
                });
            }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Получить статус рассылки",
            Description = "Возвращает детальную информацию о рассылке по её ID.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Информация получена", typeof(BroadcastSummaryDto))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Нет прав для просмотра этой рассылки")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Рассылка не найдена")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetBroadcastStatusAsync(
            [SwaggerParameter(Description = "Уникальный идентификатор рассылки (GUID)")] Guid id)
        {
            try
            {
                var userId = await GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                var summary = await _service.GetBroadcastSummaryAsync(id, userId, isAdmin);

                if (summary == null)
                    return NotFound("Рассылка не найдена");

                return Ok(summary);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Ошибка при получении статуса рассылки"
                });
            }
        }

        [HttpPost("{id}/read")]
        [SwaggerOperation(
            Summary = "Отметить рассылку как прочитанную",
            Description = "Устанавливает статус 'Прочитано' для текущего пользователя в конкретной рассылке.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Статус обновлен", typeof(bool))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Рассылка не найдена")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> MarkAsReadAsync(
            [SwaggerParameter(Description = "Уникальный идентификатор рассылки (GUID)")] Guid id)
        {
            try
            {
                var userId = await GetCurrentUserId();
                var result = await _service.MarkAsReadAsync(id, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Ошибка при отметке прочтения"
                });
            }
        }

        [HttpGet("my")]
        [SwaggerOperation(
            Summary = "Получить список моих рассылок", 
            Description = "Возвращает список рассылок, адресованных текущему пользователю.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Список получен", typeof(IEnumerable<object>))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMyBroadcastsAsync(
            [FromQuery][SwaggerParameter("Фильтр: только непрочитанные", Required = false)] bool unreadOnly = true)
        {
            try
            {
                var userId = await GetCurrentUserId();
                var broadcasts = await _service.GetMyBroadcastsAsync(userId, unreadOnly);
                return Ok(broadcasts);
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Ошибка при получении ваших рассылок"
                });
            }
        }
    }
}
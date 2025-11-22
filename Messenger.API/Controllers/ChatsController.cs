using Messenger.Core.DTOs.Chats;
using Messenger.Core.Interfaces;
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
    [SwaggerTag("Контроллер для управления чатами")]
    public class ChatsController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatsController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet()]
        [SwaggerOperation(
            Summary = "Получить чаты пользователя",
            Description = "Возвращает список всех чатов, в которых состоит указанный пользователь.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetChatsByIdAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var chats = await _chatService.GetUserChatsAsync(userId, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Data = chats
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

        [HttpPost("create-chat")]
        [SwaggerOperation(
            Summary = "Создать новый чат",
            Description = "Создает новый чат от имени текущего авторизованного пользователя.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> CreateNewChatAsync([FromBody] CreateChatRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { IsSuccess = false, Error = "Название чата обязательно" });

            if (request.UserIds == null || request.UserIds.Count == 0)
                return BadRequest(new { IsSuccess = false, Error = "Выберите хотя бы одного участника" });

            if (request.Type == "private" && request.UserIds.Count != 1)
                return BadRequest(new { IsSuccess = false, Error = "Приватный чат должен содержать ровно одного участника" });

            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var chat = await _chatService.CreateChatAsync(request.Name, request.Type, userId, cancellationToken);

                foreach (var uid in request.UserIds)
                {
                    if (uid != userId)
                        await _chatService.AddParticipantToChatAsync(chat.ChatId, uid, cancellationToken);
                }

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Чат успешно создан"
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

        [HttpPost("{chatId}/{userId}/participant")]
        [SwaggerOperation(
            Summary = "Добавить участника в чат",
            Description = "Добавляет указанного пользователя в чат.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> AddParticipantToChatAsync(Guid chatId, Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _chatService.AddParticipantToChatAsync(chatId, userId, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = $"Пользователь {userId} успешно добавлен в чат"
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

        [HttpPut("{chatId}")]
        [SwaggerOperation(
            Summary = "Обновить информацию о чате",
            Description = "Изменяет название и тип чата.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> UpdateChatAsync(Guid chatId, [FromBody] UpdateChatRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { IsSuccess = false, Error = "Название не может быть пустым" });

            try
            {
                var chat = await _chatService.GetChatByIdAsync(chatId, ct);
                if (chat == null) 
                    return NotFound(new { IsSuccess = false, Error = "Чат не найден" });

                chat.Name = request.Name;
                await _chatService.UpdateChatAsync(chat, ct);

                return Ok(new { IsSuccess = true, Message = "Чат обновлён" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, Error = ex.Message });
            }
        }

        [HttpDelete("{chatId}")]
        [SwaggerOperation(
            Summary = "Удалить чат",
            Description = "Удаляет чат и всех его участников.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> DeleteChatAsync(Guid chatId, CancellationToken cancellationToken = default)
        {
            try
            {
                var chat = await _chatService.GetChatByIdAsync(chatId, cancellationToken)
                    ?? throw new Exception("Чат не найден");

                await _chatService.DeleteChatAsync(chat, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Чат успешно удален"
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

        [HttpDelete("{chatId}/{userId}")]
        [SwaggerOperation(
            Summary = "Удалить участника из чата",
            Description = "Удаляет указанного пользователя из чата.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> DeleteParticipantsFromChatAsync(Guid chatId, Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var chatParticipants = await _chatService.GetChatParticipantsAsync(chatId, cancellationToken);

                await _chatService.DeleteParticipantFromChatAsync(chatId, userId, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = $"Пользователь {userId} успешно удален из чата"
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

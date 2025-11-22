using Messenger.Core.DTOs.Messages;
using Messenger.Core.Hubs;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;
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
    [SwaggerTag("Контроллер для управления сообщениями")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IMessageStatusService _messageStatusService;
        private readonly IReactionService _reactionService;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessagesController(IMessageService messageService, IMessageStatusService messageStatusService, 
            IReactionService reactionService, IHubContext<ChatHub> hubContext)
        {
            _messageService = messageService;
            _messageStatusService = messageStatusService;
            _reactionService = reactionService;
            _hubContext = hubContext;
        }

        [HttpPost("{chatId}")]
        [SwaggerOperation(
            Summary = "Отправить сообщение",
            Description = "Отправляет новое сообщение в указанный чат. Требуется авторизация.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> SendMessageAsync(Guid chatId, [FromForm] string? messageText,
            [FromForm] IFormFile[]? files, CancellationToken cancellationToken = default)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { isSuccess = false, error = "Пользователь не авторизован." });

                if (chatId == Guid.Empty)
                    return BadRequest(new { isSuccess = false, error = "Некорректный идентификатор чата." });

                if (string.IsNullOrWhiteSpace(messageText) && (files == null || files.Length == 0))
                    return BadRequest(new { isSuccess = false, error = "Пустое сообщение." });

                if (!Guid.TryParse(userIdClaim, out Guid senderId))
                    return Unauthorized(new { isSuccess = false, error = "Некорректный идентификатор пользователя." });

                var hasAttachments = files != null && files.Length > 0;

                var result = await _messageService.SendMessageAsync(
                    chatId, senderId, null,
                    messageText, hasAttachments, cancellationToken);

                if (!result.isSuccess)
                    return BadRequest(new
                    {
                        isSuccess = false,
                        error = result.error,
                        innerError = result.innerError
                    });

                await _hubContext.Clients.Group(chatId.ToString())
                    .SendAsync("ReceiveMessage", result.data);

                return Ok(new { isSuccess = true, data = result.data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isSuccess = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("{chatId}")]
        [SwaggerOperation(
            Summary = "Получить сообщения чата",
            Description = "Возвращает список сообщений для указанного чата.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetMessagesByChatAsync(Guid chatId, CancellationToken cancellationToken = default)
        {
            try
            {
                var messages = await _messageService.GetMessagesAsync(chatId, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Data = messages
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

        [HttpPost("{messageId}/status")]
        [SwaggerOperation(
            Summary = "Добавить или обновить статус сообщения",
            Description = "Обновляет статус сообщения (прочитано, доставлено и т.п.) для текущего пользователя.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> AddOrUpdateMessageStatusAsync(Guid messageId, 
            [FromBody] UpdateMessageStatusRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var messageStatus = new MessageStatus
                {
                    MessageId = messageId,
                    UserId = userId,
                    Status = request.Status,
                    ChangeDate = DateTime.UtcNow
                };
                await _messageStatusService.AddOrUpdateStatusAsync(messageStatus, cancellationToken);

                return Ok(new 
                { 
                    IsSuccess = true, 
                    Message = "Статус сообщения успешно обновлен" 
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

        [HttpGet("{messageId}/statuses")]
        [SwaggerOperation(
            Summary = "Получить статусы сообщения",
            Description = "Возвращает все статусы для указанного сообщения (например, кто прочитал).")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetMessageStatusesAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var statuses = await _messageStatusService.GetStatusesByMessageIdAsync(messageId, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Data = statuses
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

        [HttpPost("{messageId}/reaction")]
        [SwaggerOperation(
            Summary = "Добавить реакцию на сообщение",
            Description = "Добавляет реакцию (эмодзи) к сообщению.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> AddReactionAsync(Guid messageId, [FromBody] AddReactionRequest request, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var reaction = new Reaction
                {
                    ReactionId = Guid.NewGuid(),
                    MessageId = messageId,
                    UserId = userId,
                    ReactionType = request.ReactionType
                };
                await _reactionService.AddReactionAsync(reaction, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Message = "Реакция на сообщения успешно добавлена"
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

        [HttpPut("{messageId}")]
        [SwaggerOperation(
            Summary = "Обновить сообщение",
            Description = "Позволяет изменить содержимое сообщения (например, отредактировать текст).")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> UpdateMessageAsync(Guid messageId, [FromBody] UpdateMessageRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.MessageText))
                return BadRequest(new { IsSuccess = false, Error = "Текст не может быть пустым" });

            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var message = await _messageService.GetMessageByIdAsync(request.ChatId, messageId, ct);
                if (message == null) 
                    return NotFound(new { IsSuccess = false, Error = "Сообщение не найдено" });
                if (message.SenderId != userId) 
                    return Forbid();

                message.MessageText = request.MessageText;
                await _messageService.UpdateMessageAsync(message, ct);

                await _hubContext.Clients.Group(request.ChatId.ToString())
                    .SendAsync("ReceiveMessage", message);

                return Ok(new { IsSuccess = true, Message = "Сообщение обновлено" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, Error = ex.Message });
            }
        }

        [HttpGet("{messageId}/reactions")]
        [SwaggerOperation(
            Summary = "Получить реакции на сообщение",
            Description = "Возвращает список всех реакций (эмодзи) на указанное сообщение.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetReactionsAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var reactions = await _reactionService.GetReactionsByMessageIdAsync(messageId, cancellationToken);

                return Ok(new
                {
                    IsSuccess = true,
                    Data = reactions
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

        [HttpDelete("{messageId}")]
        [SwaggerOperation(
            Summary = "Удалить сообщение",
            Description = "Удаляет сообщение по его идентификатору.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> DeleteMessageAsync(Guid messageId, [FromQuery] Guid chatId, CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var message = await _messageService.GetMessageByIdAsync(chatId, messageId, ct);
                if (message == null) 
                    return NotFound();
                if (message.SenderId != userId)
                    return Forbid();

                await _messageService.DeleteMessageAsync(messageId, ct);
                await _hubContext.Clients.Group(chatId.ToString())
                    .SendAsync("MessageDeleted", new { messageId });

                return Ok(new { IsSuccess = true, Message = "Сообщение удалено" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, Error = ex.Message });
            }
        }
    }
}

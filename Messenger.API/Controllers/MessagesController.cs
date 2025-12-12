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
        private readonly IAttachmentService _attachmentService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IChatService _chatService;
        private readonly IUserService _userService;

        public MessagesController(IMessageService messageService, IMessageStatusService messageStatusService, 
            IReactionService reactionService, IHubContext<ChatHub> hubContext, IAttachmentService attachmentService, 
            IChatService chatService, IUserService userService)
        {
            _messageService = messageService;
            _messageStatusService = messageStatusService;
            _reactionService = reactionService;
            _hubContext = hubContext;
            _attachmentService = attachmentService;
            _chatService = chatService;
            _userService = userService;
        }

        [HttpPost("{chatId}")]
        [SwaggerOperation(
            Summary = "Отправить сообщение",
            Description = "Отправляет новое сообщение в указанный чат. Требуется авторизация.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> SendMessageAsync(Guid chatId, [FromForm] string? messageText, [FromForm] string? senderName,
            [FromForm] IFormFile[]? files, CancellationToken cancellationToken = default)
        {
            try
            {
                var senderId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var chat = await _chatService.GetChatByIdAsync(chatId, cancellationToken);
                if (chat == null)
                    return NotFound(new { error = "Чат не найден" });

                if (!chat.ChatParticipants.Any(p => p.UserId == senderId))
                    return Forbid();

                if (chat.Type == "private")
                {
                    var recipientId = chat.ChatParticipants.First(p => p.UserId != senderId).UserId;

                    if (await _userService.IsBlockedByAsync(recipientId, senderId, cancellationToken))
                    {
                        return BadRequest(new
                        {
                            error = "Вы не можете отправлять сообщения этому пользователю — вы в его чёрном списке"
                        });
                    }
                }

                var result = await _messageService.SendMessageAsync(
                    chatId: chatId,
                    senderId: senderId,
                    receiverId: null,
                    content: messageText?.Trim(),
                    hasAttachments: files?.Any() == true,
                    files: files,
                    token: cancellationToken
                );

                if (!result.isSuccess)
                    return BadRequest(new { error = result.error });

                var message = result.data!;

                var attachments = new List<AttachmentDto>();
                if (files != null && files.Length > 0)
                {
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    Directory.CreateDirectory(uploadPath);

                    foreach (var file in files.Where(f => f.Length > 0))
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadPath, fileName);
                        var fileUrl = $"/uploads/{fileName}";

                        await using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream, cancellationToken);

                        var attachment = new Attachment
                        {
                            AttachmentId = Guid.NewGuid(),
                            MessageId = message.MessageId,
                            FileName = file.FileName,
                            FileType = file.ContentType ?? "application/octet-stream",
                            SizeInBytes = (int)file.Length,
                            Url = fileUrl
                        };

                        await _attachmentService.AddAttachmentAsync(attachment, cancellationToken);

                        attachments.Add(new AttachmentDto
                        {
                            AttachmentId = attachment.AttachmentId,
                            FileName = attachment.FileName,
                            FileType = attachment.FileType,
                            SizeInBytes = attachment.SizeInBytes ?? 0,
                            Url = attachment.Url
                        });
                    }
                }

                var messageDto = new MessageDto
                {
                    MessageId = message.MessageId,
                    ChatId = message.ChatId,
                    SenderId = senderId,
                    SenderName = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Пользователь",
                    MessageText = message.MessageText,
                    SentAt = message.SendTime,
                    Status = "Sent",
                    Attachments = attachments
                };

                await _hubContext.Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", messageDto, cancellationToken);

                return Ok(new { isSuccess = true, data = messageDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, error = ex.Message });
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

                var dtos = messages.Select(m => new MessageDto
                {
                    MessageId = m.MessageId,
                    ChatId = m.ChatId == Guid.Empty ? chatId : m.ChatId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender != null
                        ? $"{m.Sender.FirstName} {m.Sender.LastName}".Trim()
                        : "Удалённый пользователь",
                    MessageText = m.MessageText,
                    SentAt = m.SendTime,
                    Status = "Read",
                    Attachments = m.Attachments.Select(a => new AttachmentDto
                    {
                        AttachmentId = a.AttachmentId,
                        FileName = a.FileName,
                        FileType = a.FileType ?? GetMimeType(a.FileName),
                        SizeInBytes = a.SizeInBytes ?? 0,
                        Url = a.Url
                    }).ToList()
                }).ToList();

                return Ok(new
                {
                    IsSuccess = true,
                    Data = dtos
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

        private static string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
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

using MassTransit;
using Messenger.API.Responses;
using Messenger.API.Services;
using Messenger.Core.DTOs.Messages;
using Messenger.Core.Hubs;
using Messenger.Core.Interfaces;
using Messenger.Core.Messages;
using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Swashbuckle.AspNetCore.Annotations;

namespace Messenger.API.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [SwaggerTag("Контроллер для управления сообщениями")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IReactionService _reactionService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<MessagesController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly GuapMessengerContext _context;

        public MessagesController(IMessageService messageService,
            IReactionService reactionService, IHubContext<ChatHub> hubContext, IChatService chatService, 
            IUserService userService, IEncryptionService encryptionService, ILogger<MessagesController> logger,
            IPublishEndpoint publishEndpoint, GuapMessengerContext context)
        {
            _messageService = messageService;
            _reactionService = reactionService;
            _hubContext = hubContext;
            _chatService = chatService;
            _userService = userService;
            _encryptionService = encryptionService;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
            _context = context;
        }

        [HttpGet("{chatId}/search")]
        [SwaggerOperation(
            Summary = "Поиск сообщения по названию в чате",
            Description = "Возращает найденное сообщение по критерию поиска")]
        [SwaggerResponse(StatusCodes.Status200OK, "Сообщение успешно найдено по критериям", typeof(MessageSearchResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные или пользователь заблокирован", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> SearchMessages([SwaggerParameter(Description = "Идентификатор чата (GUID)")] Guid chatId,
            [FromQuery][SwaggerParameter(Description = "Критерий поиска (название сообщения)")] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new MessageSearchResponse
                {
                    IsSuccess = true,
                    Data = new List<MessageDto>()
                });
            }

            try
            {
                var results = await _messageService.SearchMessagesAsync(chatId, query.Trim());
                return Ok(new MessageSearchResponse
                {
                    IsSuccess = true,
                    Data = results
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

        [HttpPost("{chatId}")]
        [SwaggerOperation(
            Summary = "Отправить сообщение в чат",
            Description = "Отправляет текстовое сообщение и/или файлы (вложения) в указанный чат. " +
                "Сообщение сохраняется асинхронно через consumer и рассылается через SignalR.")]
        [Consumes("multipart/form-data", "application/json")]
        [SwaggerResponse(StatusCodes.Status200OK, "Сообщение успешно отправлено", typeof(SendMessageSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные или пользователь заблокирован", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Доступ запрещён — пользователь не является участником чата")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Чат не найден", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> SendMessageAsync(
            [SwaggerParameter(Description = "Идентификатор чата (GUID)")] Guid chatId,
            [FromForm][SwaggerParameter(Description = "Текст сообщения (опционально)")] string? messageText,
            [FromForm][SwaggerParameter(Description = "Файлы-вложения (опционально, несколько файлов)")] IFormFile[]? files,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (user, error) = await UserValidationService.GetCurrentUserOrErrorAsync(User, _userService);
                if (error != null)
                    return error;

                const long MAX_FILE_SIZE = 10 * 1024 * 1024;
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        if (file.Length > MAX_FILE_SIZE)
                        {
                            return BadRequest(new ErrorResponse
                            {
                                IsSuccess = false,
                                Error = $"Файл '{file.FileName}' превышает максимальный размер 10 МБ. " +
                                        $"Текущий размер: {file.Length / (1024 * 1024):F2} МБ"
                            });
                        }
                    }
                }

                var chat = await _chatService.GetChatByIdAsync(chatId, cancellationToken);
                if (chat == null)
                {
                    return NotFound(new ErrorResponse { Error = "Чат не найден" });
                }

                if (!chat.ChatParticipants.Any(p => p.UserId == user!.UserId))
                {
                    return Forbid();
                }

                if (chat.Type == "private")
                {
                    var recipientId = chat.ChatParticipants.FirstOrDefault(p => p.UserId != user!.UserId)?.UserId;
                    if (recipientId != null)
                    {
                        bool blockedByRecipient = await _userService.IsBlockedByAsync(recipientId.Value, user!.UserId, cancellationToken);
                        bool blockedByMe = await _userService.IsBlockedByAsync(user!.UserId, recipientId.Value, cancellationToken);

                        if (blockedByRecipient || blockedByMe)
                        {
                            return BadRequest(new ErrorResponse
                            {
                                IsSuccess = false,
                                Error = blockedByMe
                                    ? "Вы не можете отправить сообщение, так как заблокировали этого пользователя."
                                    : "Вы не можете отправлять сообщения этому пользователю — вы в его чёрном списке."
                            });
                        }
                    }
                }

                var contentToSave = string.IsNullOrWhiteSpace(messageText)
                    ? null
                    : _encryptionService.Encrypt(messageText.Trim());

                var attachmentsInfo = new List<AttachmentInfo>();
                var attachmentDtos = new List<AttachmentDto>();

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

                        var attachmentId = Guid.NewGuid();

                        attachmentsInfo.Add(new AttachmentInfo
                        {
                            AttachmentId = attachmentId,
                            FileName = file.FileName,
                            FileType = file.ContentType ?? "application/octet-stream",
                            SizeInBytes = file.Length,
                            Url = fileUrl
                        });

                        attachmentDtos.Add(new AttachmentDto
                        {
                            AttachmentId = attachmentId,
                            FileName = file.FileName,
                            FileType = file.ContentType ?? "application/octet-stream",
                            SizeInBytes = (int)file.Length,
                            Url = fileUrl
                        });
                    }
                }

                var messageId = Guid.NewGuid();

                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var encryptedText = string.IsNullOrWhiteSpace(messageText)
                        ? null
                        : _encryptionService.Encrypt(messageText.Trim());

                    await _publishEndpoint.Publish(new ChatMessageSent
                    {
                        MessageId = messageId,
                        ChatId = chatId,
                        SenderId = user!.UserId,
                        SenderName = $"{user.FirstName} {user.LastName}".Trim(),
                        MessageText = encryptedText,
                        SentAt = DateTime.UtcNow,
                        HasAttachments = attachmentsInfo.Count > 0,
                        Attachments = attachmentsInfo
                    }, cancellationToken);

                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    var decryptedText = string.IsNullOrEmpty(encryptedText) ? null
                        : _encryptionService.TryDecryptSafe(encryptedText);

                    var finalMessageDto = new MessageDto
                    {
                        MessageId = messageId,
                        ChatId = chatId,
                        SenderId = user.UserId,
                        SenderName = $"{user.FirstName} {user.LastName}".Trim(),
                        MessageText = decryptedText,
                        SentAt = DateTime.UtcNow,
                        Status = "Sent",
                        Attachments = attachmentDtos
                    };

                    await _hubContext.Clients.Group(chatId.ToString())
                        .SendAsync("ReceiveMessage", finalMessageDto);

                    await _hubContext.Clients.Group(chatId.ToString())
                        .SendAsync("MessageSendingStatus", new
                        {
                            MessageId = messageId,
                            ChatId = chatId,
                            Status = "Sent",
                            Timestamp = DateTimeOffset.UtcNow
                        });

                    _logger.LogInformation("Сообщение {MessageId} отправлено через SignalR из контроллера", messageId);

                    return Ok(new SendMessageSuccessResponse
                    {
                        IsSuccess = true,
                        Data = finalMessageDto
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Ошибка при публикации сообщения в чат {ChatId}", chatId);

                    return StatusCode(500, new ErrorResponse
                    {
                        IsSuccess = false,
                        Error = "Внутренняя ошибка сервера при отправке сообщения"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения в чат {ChatId}", chatId);
                return StatusCode(500, new ErrorResponse
                {
                    IsSuccess = false,
                    Error = ex.Message
                });
            }
        }

        [HttpGet("{chatId}")]
        [SwaggerOperation(
            Summary = "Получить сообщения чата",
            Description = "Возвращает список всех сообщений в чате с вложениями и информацией об отправителе.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Сообщения успешно получены", typeof(GetMessagesSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetMessagesByChatAsync(
            [SwaggerParameter(Description = "Идентификатор чата (GUID)")] Guid chatId, 
            CancellationToken cancellationToken = default)
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
                    MessageText = string.IsNullOrEmpty(m.MessageText) ? null
                        : _encryptionService.TryDecryptSafe(m.MessageText),
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

                return Ok(new GetMessagesSuccessResponse
                {
                    IsSuccess = true,
                    Data = dtos
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

        [HttpPost("{messageId}/reaction")]
        [SwaggerOperation(
            Summary = "Добавить реакцию на сообщение",
            Description = "Добавляет эмодзи-реакцию к сообщению от имени текущего пользователя.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Реакция добавлена", typeof(AddReactionSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> AddReactionAsync(
            [SwaggerParameter(Description = "Идентификатор сообщения")] Guid messageId, 
            [FromBody] [SwaggerParameter(Description = "Тип реакции (эмодзи)", Required = true)] AddReactionRequest request, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (user, error) = await UserValidationService.GetCurrentUserOrErrorAsync(User, _userService);
                if (error != null)
                {
                    return error;
                }

                var reaction = new Reaction
                {
                    ReactionId = Guid.NewGuid(),
                    MessageId = messageId,
                    UserId = user!.UserId,
                    ReactionType = request.ReactionType
                };
                await _reactionService.AddReactionAsync(reaction, cancellationToken);

                return Ok(new AddReactionSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Реакция на сообщения успешно добавлена"
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

        [HttpPut("{messageId}")]
        [SwaggerOperation(
            Summary = "Редактировать сообщение",
            Description = "Изменяет текст существующего сообщения. Доступно только отправителю.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Сообщение обновлено", typeof(UpdateMessageSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Текст не может быть пустым", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Редактирование запрещено — не автор сообщения")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Сообщение не найдено", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> UpdateMessageAsync(
            [SwaggerParameter(Description = "Идентификатор сообщения")] Guid messageId, 
            [FromBody] [SwaggerParameter(Description = "Новый текст сообщения и ID чата", Required = true)] UpdateMessageRequest request, 
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.MessageText))
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Текст не может быть пустым"
                });
            }

            try
            {
                var (user, error) = await UserValidationService.GetCurrentUserOrErrorAsync(User, _userService);
                if (error != null)
                {
                    return error;
                }

                var message = await _messageService.GetMessageByIdAsync(request.ChatId, messageId, ct);
                if (message == null) 
                {
                    return NotFound(new ErrorResponse
                    {
                        IsSuccess = false,
                        Error = "Сообщение не найдено"
                    });
                }
                if (message.SenderId != user!.UserId) 
                { 
                    return Forbid(); 
                }

                message.MessageText = request.MessageText;
                await _messageService.UpdateMessageAsync(message, ct);

                await _hubContext.Clients.Group(request.ChatId.ToString())
                    .SendAsync("ReceiveMessage", message);

                return Ok(new UpdateMessageSuccessResponse
                {
                    IsSuccess = true, 
                    Message = "Сообщение обновлено" 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, Error = ex.Message });
            }
        }

        [HttpGet("{messageId}/reactions")]
        [SwaggerOperation(
            Summary = "Получить реакции на сообщение",
            Description = "Возвращает все реакции (эмодзи) на указанное сообщение.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Реакции получены", typeof(GetReactionsSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetReactionsAsync(
            [SwaggerParameter(Description = "Идентификатор сообщения")] Guid messageId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var reactions = await _reactionService.GetReactionsByMessageIdAsync(messageId, cancellationToken);

                return Ok(new GetReactionsSuccessResponse
                {
                    IsSuccess = true,
                    Data = reactions
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

        [HttpDelete("{messageId}")]
        [SwaggerOperation(
            Summary = "Удалить сообщение",
            Description = "Удаляет сообщение. Доступно только отправителю. Уведомление рассылается через SignalR.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Сообщение удалено", typeof(DeleteMessageSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Удаление запрещено — не автор сообщения")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Сообщение не найдено", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteMessageAsync(
            [SwaggerParameter(Description = "Идентификатор сообщения")] Guid messageId, 
            [FromQuery] [SwaggerParameter(Description = "Идентификатор чата (обязателен для проверки прав)")] Guid chatId, 
            CancellationToken ct = default)
        {
            try
            {
                var (user, error) = await UserValidationService.GetCurrentUserOrErrorAsync(User, _userService);
                if (error != null)
                {
                    return error;
                }

                var message = await _messageService.GetMessageByIdAsync(chatId, messageId, ct);
                if (message == null) 
                { 
                    return NotFound(); 
                }
                if (message.SenderId != user!.UserId)
                { 
                    return Forbid(); 
                }

                await _messageService.DeleteMessageAsync(messageId, ct);
                await _hubContext.Clients.Group(chatId.ToString())
                    .SendAsync("MessageDeleted", new { messageId });

                return Ok(new DeleteMessageSuccessResponse
                { 
                    IsSuccess = true, 
                    Message = "Сообщение удалено" 
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

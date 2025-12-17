using Messenger.API.Responses;
using Messenger.Core.DTOs.Chats;
using Messenger.Core.Hubs;
using Messenger.Core.Interfaces;
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
    [Produces("application/json")]
    [Consumes("application/json")]
    [SwaggerTag("Контроллер для управления чатами")]
    public class ChatsController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IUserService _userService;

        public ChatsController(IChatService chatService, IHubContext<ChatHub> hubContext, IUserService userService)
        {
            _chatService = chatService;
            _hubContext = hubContext;
            _userService = userService;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Получить список чатов текущего пользователя",
            Description = "Возвращает все чаты, в которых состоит авторизованный пользователь, с информацией о последнем сообщении.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Список чатов успешно получен", typeof(GetUserChatsSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetChatsByIdAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var chats = await _chatService.GetUserChatsWithLastMessageAsync(userId, cancellationToken);

                return Ok(new GetUserChatsSuccessResponse
                {
                    IsSuccess = true,
                    Data = chats
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

        [HttpGet("{chatId}")]
        [SwaggerOperation(
            Summary = "Получить информацию о конкретном чате",
            Description = "Возвращает детали чата: название, тип, участников и аватар (для приватных чатов — аватар собеседника).")]
        [SwaggerResponse(StatusCodes.Status200OK, "Информация о чате получена", typeof(GetChatByIdSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Доступ запрещён — пользователь не является участником чата")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Чат не найден", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> GetChatByIdAsync(
            [SwaggerParameter(Description = "Уникальный идентификатор чата (GUID)")] Guid chatId, 
            CancellationToken ct = default)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var chat = await _chatService.GetChatByIdAsync(chatId, ct);
                if (chat == null)
                {
                    return NotFound(new ErrorResponse
                    { 
                        IsSuccess = false, 
                        Error = "Чат не найден" 
                    });
                }

                var participants = await _chatService.GetChatParticipantsAsync(chatId, ct);
                if (!participants.Any(p => p.UserId == currentUserId))
                    return Forbid();

                var participantDtos = participants.Select(p => new
                {
                    id = p.UserId,
                    name = $"{p.User?.FirstName} {p.User?.LastName}".Trim(),
                    avatar = p.User?.Account?.Avatar
                }).ToList();

                string displayName = chat.Type == "group"
                    ? chat.Name
                    : await GetPrivateChatDisplayNameAsync(chatId, currentUserId, ct);

                var result = new
                {
                    chatId = chat.ChatId,
                    name = displayName,
                    type = chat.Type,
                    avatar = chat.Type == "group" ? (string?)null : await GetOtherUserAvatarAsync(participantDtos.FirstOrDefault(p => p.id != currentUserId)?.id ?? currentUserId, ct),
                    participants = participantDtos
                };

                return Ok(new GetChatByIdSuccessResponse 
                { 
                    IsSuccess = true, 
                    Data = result 
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

        [HttpPost("create-chat")]
        [SwaggerOperation(
            Summary = "Создать новый чат",
            Description = "Создаёт приватный или групповой чат. Для приватного — ровно один участник, для группового — название обязательно.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Чат успешно создан", typeof(CreateChatSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные запроса", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> CreateNewChatAsync(
            [FromBody] [SwaggerParameter(Description = "Данные для создания чата", Required = true)] CreateChatRequest request, 
            CancellationToken ct = default)
        {
            if (!new[] { "private", "group" }.Contains(request.Type))
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Тип чата должен быть 'private' или 'group'"
                });
            }

            if (request.Type == "group" && string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Для группового чата укажите название"
                });
            }

            if (request.UserIds == null || request.UserIds.Count == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Выберите хотя бы одного участника"
                });
            }

            if (request.Type == "private" && request.UserIds.Count != 1)
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "В приватном чате должен быть ровно один участник"
                });
            }

            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                string chatNameForDb = request.Type == "group"
                    ? request.Name.Trim()
                    : "Приватный чат";

                var chat = await _chatService.CreateChatAsync(
                    name: chatNameForDb,
                    type: request.Type,
                    creatorId: currentUserId,
                    ct);

                foreach (var userId in request.UserIds.Distinct())
                {
                    if (userId == currentUserId) 
                        continue;

                    await _chatService.AddParticipantToChatAsync(chat.ChatId, userId, "участник", ct);
                }

                string displayName = request.Type == "group"
                    ? request.Name.Trim()
                    : await GetPrivateChatDisplayNameAsync(chat.ChatId, currentUserId, ct);

                var chatForClients = new
                {
                    chatId = chat.ChatId,
                    name = displayName,
                    avatar = request.Type == "private"
                        ? await GetOtherUserAvatarAsync(request.UserIds[0], ct)
                        : (string?)null,
                    type = chat.Type
                };

                var allUsers = request.UserIds.Append(currentUserId).Distinct();
                foreach (var userId in allUsers)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("NewChat", chatForClients, ct);
                }

                return Ok(new CreateChatSuccessResponse 
                { 
                    IsSuccess = true, 
                    Data = new 
                    { 
                        chat.ChatId, 
                        Name = displayName 
                    } 
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

        private async Task<string> GetPrivateChatDisplayNameAsync(Guid chatId, Guid currentUserId, CancellationToken ct)
        {
            var participants = await _chatService.GetChatParticipantsAsync(chatId, ct);

            var otherParticipant = participants
                .FirstOrDefault(p => p.UserId != currentUserId);

            if (otherParticipant?.User == null)
                return "Удалённый пользователь";

            return $"{otherParticipant.User.FirstName} {otherParticipant.User.LastName}".Trim();
        }

        private async Task<string?> GetOtherUserAvatarAsync(Guid userId, CancellationToken ct)
        {
            var user = await _userService.GetUserByIdAsync(userId, ct);
            return user?.Account?.Avatar;
        }

        [HttpPost("{chatId}/{userId}/participant")]
        [SwaggerOperation(
            Summary = "Добавить участника в чат",
            Description = "Добавляет пользователя в существующий чат и уведомляет всех участников через SignalR.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Участник успешно добавлен", typeof(AddParticipantSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Пользователь или чат не найден", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> AddParticipantToChatAsync(
            [SwaggerParameter(Description = "Идентификатор чата")] Guid chatId,
            [SwaggerParameter(Description = "Идентификатор добавляемого пользователя")] Guid userId,
            [SwaggerParameter(Description = "Роль в чате (по умолчанию 'участник')")] string role = "участник",
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
                if (user == null)
                { 
                    return NotFound(new ErrorResponse 
                    { 
                        IsSuccess = false, 
                        Error = "Пользователь не найден" 
                    }); 
                }

                await _chatService.AddParticipantToChatAsync(chatId, userId, role, cancellationToken);

                var userInfo = new
                {
                    id = user.UserId,
                    name = $"{user.FirstName} {user.LastName}".Trim(),
                    fullName = $"{user.FirstName} {user.LastName}".Trim(),
                    avatar = user.Account?.Avatar
                };

                await _hubContext.Clients.Group(chatId.ToString()).SendAsync("ParticipantAdded", new
                {
                    chatId,
                    user = userInfo
                }, cancellationToken);

                var chat = await _chatService.GetChatByIdAsync(chatId, cancellationToken);
                string displayName = chat.Type == "group"
                    ? chat.Name
                    : await GetPrivateChatDisplayNameAsync(chatId, userId, cancellationToken);

                var chatForNewUser = new
                {
                    chatId = chat.ChatId,
                    name = displayName,
                    avatar = chat.Type == "private"
                        ? await GetOtherUserAvatarAsync(userId, cancellationToken)
                        : (string?)null,
                    type = chat.Type
                };

                await _hubContext.Clients.User(userId.ToString()).SendAsync("NewChat", chatForNewUser, cancellationToken);

                return Ok(new AddParticipantSuccessResponse
                {
                    IsSuccess = true,
                    Message = $"Пользователь {userId} успешно добавлен в чат"
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

        [HttpPut("{chatId}")]
        [SwaggerOperation(
            Summary = "Обновить название чата",
            Description = "Изменяет название группового чата.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Чат успешно обновлён", typeof(UpdateChatSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Название не может быть пустым", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Чат не найден", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> UpdateChatAsync(
            [SwaggerParameter(Description = "Идентификатор чата")] Guid chatId, 
            [FromBody] [SwaggerParameter(Description = "Новые данные чата", Required = true)] UpdateChatRequest request, 
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new ErrorResponse
                {
                    IsSuccess = false,
                    Error = "Название не может быть пустым"
                });
            }

            try
            {
                var chat = await _chatService.GetChatByIdAsync(chatId, ct);
                if (chat == null) 
                {
                    return NotFound(new ErrorResponse
                    {
                        IsSuccess = false,
                        Error = "Чат не найден"
                    });
                }

                chat.Name = request.Name;
                await _chatService.UpdateChatAsync(chat, ct);

                return Ok(new UpdateChatSuccessResponse
                { 
                    IsSuccess = true, 
                    Message = "Чат обновлён" 
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

        [HttpDelete("{chatId}")]
        [SwaggerOperation(
            Summary = "Удалить чат",
            Description = "Полностью удаляет чат и все связанные данные.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Чат успешно удалён", typeof(DeleteChatSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Чат не найден", typeof(ErrorResponse))]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteChatAsync(
            [SwaggerParameter(Description = "Идентификатор удаляемого чата")] Guid chatId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var chat = await _chatService.GetChatByIdAsync(chatId, cancellationToken);
                if (chat == null)
                {
                    return NotFound(new ErrorResponse
                    {
                        IsSuccess = false,
                        Error = "Чат не найден"
                    });
                }

                await _chatService.DeleteChatAsync(chat, cancellationToken);

                return Ok(new DeleteChatSuccessResponse
                {
                    IsSuccess = true,
                    Message = "Чат успешно удален"
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

        [HttpDelete("{chatId}/{userId}")]
        [SwaggerOperation(
            Summary = "Удалить участника из чата",
            Description = "Исключает пользователя из чата и отправляет соответствующие SignalR-уведомления.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Участник успешно удалён", typeof(RemoveParticipantSuccessResponse))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не авторизован")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", typeof(ErrorResponse))]
        public async Task<IActionResult> DeleteParticipantsFromChatAsync(
            [SwaggerParameter(Description = "Идентификатор чата")] Guid chatId,
            [SwaggerParameter(Description = "Идентификатор удаляемого участника")] Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var chatParticipants = await _chatService.GetChatParticipantsAsync(chatId, cancellationToken);

                await _chatService.DeleteParticipantFromChatAsync(chatId, userId, cancellationToken);

                await _hubContext.Clients.Group(chatId.ToString())
                    .SendAsync("ParticipantRemoved", new { chatId, userId }, cancellationToken);

                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("YouWereRemovedFromChat", chatId, cancellationToken);

                return Ok(new RemoveParticipantSuccessResponse
                {
                    IsSuccess = true,
                    Message = $"Пользователь {userId} успешно удален из чата"
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

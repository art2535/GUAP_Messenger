using Messenger.Core.DTOs.Chats;
using Messenger.Core.Hubs;
using Messenger.Core.Interfaces;
using Messenger.Infrastructure.Services;
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
            Summary = "Получить чаты пользователя",
            Description = "Возвращает список всех чатов, в которых состоит указанный пользователь.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetChatsByIdAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var chats = await _chatService.GetUserChatsWithLastMessageAsync(userId, cancellationToken);

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

        [HttpGet("{chatId}")]
        [SwaggerOperation(
            Summary = "Получить информацию о конкретном чате",
            Description = "Возвращает детали чата: название, тип, участников, аватар и т.д.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetChatByIdAsync(Guid chatId, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var chat = await _chatService.GetChatByIdAsync(chatId, ct);
                if (chat == null)
                    return NotFound(new { IsSuccess = false, Error = "Чат не найден" });

                // Проверяем, состоит ли пользователь в чате
                var participants = await _chatService.GetChatParticipantsAsync(chatId, ct);
                if (!participants.Any(p => p.UserId == currentUserId))
                    return Forbid();

                // Формируем список участников
                var participantDtos = participants.Select(p => new
                {
                    id = p.UserId,
                    name = $"{p.User?.FirstName} {p.User?.LastName}".Trim(),
                    avatar = p.User?.Account?.Avatar
                }).ToList();

                // Для приватного чата — имя собеседника
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

                return Ok(new { IsSuccess = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsSuccess = false, Error = ex.Message });
            }
        }

        [HttpPost("create-chat")]
        [SwaggerOperation(
            Summary = "Создать новый чат",
            Description = "Создает новый чат от имени текущего авторизованного пользователя.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> CreateNewChatAsync([FromBody] CreateChatRequest request, 
            CancellationToken ct = default)
        {
            if (!new[] { "private", "group" }.Contains(request.Type))
                return BadRequest(new { IsSuccess = false, Error = "Тип чата должен быть 'private' или 'group'" });

            if (request.Type == "group" && string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { IsSuccess = false, Error = "Для группового чата укажите название" });

            if (request.UserIds == null || request.UserIds.Count == 0)
                return BadRequest(new { IsSuccess = false, Error = "Выберите хотя бы одного участника" });

            if (request.Type == "private" && request.UserIds.Count != 1)
                return BadRequest(new { IsSuccess = false, Error = "В приватном чате должен быть ровно один участник" });

            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // ЯВНО задаём имя чата — никогда не null!
                string chatNameForDb = request.Type == "group"
                    ? request.Name.Trim()
                    : "Приватный чат";

                // Создаём чат — создатель уже добавлен внутри сервиса
                var chat = await _chatService.CreateChatAsync(
                    name: chatNameForDb,
                    type: request.Type,
                    creatorId: currentUserId,
                    ct);

                // Добавляем только выбранного пользователя (для private — одного, для group — всех)
                foreach (var userId in request.UserIds.Distinct())
                {
                    if (userId == currentUserId) continue; // на всякий пожарный

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

                // Рассылаем всем (включая себя)
                var allUsers = request.UserIds.Append(currentUserId).Distinct();
                foreach (var userId in allUsers)
                {
                    await _hubContext.Clients.User(userId.ToString()).SendAsync("NewChat", chatForClients);
                }

                return Ok(new { IsSuccess = true, Data = new { chat.ChatId, Name = displayName } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    Error = ex.Message,
                    Inner = ex.InnerException?.Message,
                    StackTrace = ex.StackTrace
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
            Description = "Добавляет указанного пользователя в чат.")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> AddParticipantToChatAsync(Guid chatId, Guid userId, string role = "участник",
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _chatService.AddParticipantToChatAsync(chatId, userId, role, cancellationToken);

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

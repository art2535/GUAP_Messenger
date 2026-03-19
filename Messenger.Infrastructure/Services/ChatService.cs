using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private readonly ChatRepository _repository;
        private readonly IUserService _userService;
        private readonly IEncryptionService _encryptionService;

        public ChatService(ChatRepository repository, IUserService userService, IEncryptionService encryptionService)
        {
            _repository = repository;
            _userService = userService;
            _encryptionService = encryptionService;
        }

        public async Task<Chat> CreateChatAsync(string name, string type, Guid creatorId, CancellationToken token = default)
        {
            var chat = new Chat
            {
                ChatId = Guid.NewGuid(),
                Name = name,
                Type = type,
                UserId = creatorId,
                CreationDate = DateTime.Now
            };

            var participant = new ChatParticipant
            {
                ChatId = chat.ChatId,
                UserId = creatorId,
                Role = "владелец",
                JoinDate = DateTime.Now
            };

            await _repository.AddChatAsync(chat, token);
            await _repository.AddParticipantAsync(participant, token);

            return chat;
        }

        public async Task<List<object>> GetUserChatsWithLastMessageAsync(Guid userId, CancellationToken token = default)
        {
            var chats = await _repository.GetChatsByUserIdAsync(userId, token);

            var result = new List<object>();
            foreach (var chat in chats)
            {
                var lastMsg = chat.Messages?.OrderByDescending(m => m.SendTime).FirstOrDefault();

                bool isBlocked = false;
                if (chat.Type == "private")
                {
                    var otherParticipant = chat.ChatParticipants.FirstOrDefault(p => p.UserId != userId);
                    if (otherParticipant != null)
                    {
                        bool blockedByMe = await _userService.IsBlockedByAsync(userId, otherParticipant.UserId, token);
                        bool blockedByThem = await _userService.IsBlockedByAsync(otherParticipant.UserId, userId, token);
                        isBlocked = blockedByMe || blockedByThem;
                    }
                }

                string? decryptedLastMessage = null;
                if (lastMsg?.MessageText != null)
                {
                    try
                    {
                        decryptedLastMessage = _encryptionService.Decrypt(lastMsg.MessageText);
                    }
                    catch
                    {
                        decryptedLastMessage = "[Сообщение защищено]";
                    }
                }
                else if (chat.Messages?.Any() == true)
                {
                    decryptedLastMessage = "Вложение";
                }

                result.Add(new
                {
                    chatId = chat.ChatId,
                    name = chat.Type == "private"
                        ? chat.ChatParticipants
                            .Where(p => p.UserId != userId)
                            .Select(p => $"{p.User.FirstName} {p.User.LastName}".Trim())
                            .FirstOrDefault() ?? "Приватный чат"
                        : chat.Name,
                    avatar = chat.Type == "private"
                        ? chat.ChatParticipants
                            .Where(p => p.UserId != userId)
                            .Select(p => p.User.Account?.Avatar)
                            .FirstOrDefault()
                        : null,
                    type = chat.Type,
                    lastMessage = decryptedLastMessage,
                    isOnline = true,
                    isBlocked = isBlocked
                });
            }

            return result;
        }

        public async Task<IEnumerable<Chat>> GetUserChatsAsync(Guid userId, CancellationToken token = default)
        {
            return await _repository.GetChatsByUserIdAsync(userId, token);
        }

        public async Task AddParticipantToChatAsync(Guid chatId, Guid userId, string role, CancellationToken token = default)
        {
            var existing = await _repository.GetChatParticipantByChatAsync(chatId, userId, token);
            if (existing != null)
                return;

            var participant = new ChatParticipant
            {
                ChatId = chatId,
                UserId = userId,
                Role = role,
                JoinDate = DateTime.Now
            };

            try
            {
                await _repository.AddParticipantAsync(participant, token);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true)
            {
                return;
            }
        }

        public async Task DeleteParticipantFromChatAsync(Guid chatId, Guid userId, CancellationToken token = default)
        {
            var deleteParticipant = await _repository.GetChatParticipantByChatAsync(chatId, userId, token);

            if (deleteParticipant != null)
            {
                await _repository.DeleteParticipantAsync(deleteParticipant, token);
            }
        }

        public async Task DeleteChatAsync(Chat chat, CancellationToken token = default)
        {
            await _repository.DeleteChatAsync(chat, token);
        }

        public async Task<Chat?> GetChatByIdAsync(Guid chatId, CancellationToken token = default)
        {
            return await _repository.GetChatByIdAsync(chatId, token);
        }

        public async Task<IEnumerable<ChatParticipant>> GetChatParticipantsAsync(Guid chatId, 
            CancellationToken token = default)
        {
            return await _repository.GetParticipantsByChatAsync(chatId, token);
        }

        public async Task UpdateChatAsync(Chat chat, CancellationToken token = default)
        {
            await _repository.UpdateChatAsync(chat, token);
        }
    }
}

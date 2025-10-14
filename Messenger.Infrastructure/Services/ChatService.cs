using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;

namespace Messenger.Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private readonly ChatRepository _repository;

        public ChatService(ChatRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateChatAsync(string name, string type, Guid creatorId, CancellationToken token = default)
        {
            var chat = new Chat
            {
                ChatId = Guid.NewGuid(),
                Name = name,
                Type = type,
                UserId = creatorId,
                CreationDate = DateTime.Now
            };
            await _repository.AddChatAsync(chat, token);

            var participant = new ChatParticipant
            {
                ChatId = chat.ChatId,
                UserId = creatorId,
                Role = "владелец",
                JoinDate = DateTime.Now
            };
            await _repository.AddParticipantAsync(participant, token);
        }

        public async Task<IEnumerable<Chat>> GetUserChatsAsync(Guid userId, CancellationToken token = default)
        {
            return await _repository.GetChatsByUserIdAsync(userId, token);
        }

        public async Task AddParticipantToChatAsync(Guid chatId, Guid userId, CancellationToken token = default)
        {
            var participant = new ChatParticipant
            {
                ChatId = chatId,
                UserId = userId,
                Role = "участник",
                JoinDate = DateTime.UtcNow
            };
            await _repository.AddParticipantAsync(participant, token);
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

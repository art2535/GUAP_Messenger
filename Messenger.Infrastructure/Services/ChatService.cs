using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private readonly ChatRepository _repository;

        public ChatService(ChatRepository repository)
        {
            _repository = repository;
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

            await _repository.AddChatAsync(chat, token);
            return chat;
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

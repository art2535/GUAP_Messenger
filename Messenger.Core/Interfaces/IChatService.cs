using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IChatService
    {
        Task CreateChatAsync(string name, string type, Guid creatorId, CancellationToken token = default);
        Task<IEnumerable<Chat>> GetUserChatsAsync(Guid userId, CancellationToken token = default);
        Task AddParticipantToChatAsync(Guid chatId, Guid userId, CancellationToken token = default);
        Task DeleteParticipantFromChatAsync(Guid chatId, Guid userId, CancellationToken token = default);
        Task<IEnumerable<ChatParticipant>> GetChatParticipantsAsync(Guid chatId, CancellationToken token = default);
        Task UpdateChatAsync(Chat chat, CancellationToken token = default);
    }
}

using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IMessageService
    {
        Task SendMessageAsync(Guid chatId, Guid senderId, Guid receiverId, string content, 
            bool hasAttachments, CancellationToken token = default);
        Task<IEnumerable<Message>> GetMessagesAsync(Guid chatId, CancellationToken token = default);
        Task<Message?> GetMessageByIdAsync(Guid chatId, Guid messageId, CancellationToken token = default);
        Task DeleteMessageAsync(Guid messageId, CancellationToken token = default);
        Task UpdateMessageAsync(Message message, CancellationToken token = default);
    }
}

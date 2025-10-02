using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IMessageService
    {
        Task SendMessageAsync(Guid chatId, Guid senderId, Guid receiverId, string content, 
            bool hasAttachments, CancellationToken token = default);
        Task<IEnumerable<Message>> GetMessagesAsync(Guid chatId, CancellationToken token = default);
    }
}

using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IMessageStatusService
    {
        Task AddOrUpdateStatusAsync(MessageStatus messageStatus, CancellationToken cancellationToken = default);
        Task<IEnumerable<MessageStatus>> GetStatusesByMessageIdAsync(Guid messageId,
            CancellationToken cancellationToken = default);
    }
}

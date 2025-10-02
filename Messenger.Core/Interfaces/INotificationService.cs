using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(Guid userId, string text, CancellationToken token = default);
        Task<IEnumerable<Notification>> GetNotificationsAsync(Guid userId, CancellationToken token = default);
    }
}

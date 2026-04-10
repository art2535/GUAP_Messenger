using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface INotificationService
    {
        Task<Guid> CreateNotificationAsync(Guid userId, string text, CancellationToken token = default);
        Task<IEnumerable<Notification>> GetNotificationsAsync(Guid userId, CancellationToken token = default);
        Task<Notification?> GetNotificationAsync(Guid notificationId, CancellationToken token = default);
        Task MarkAsReadAsync(Guid notificationId, CancellationToken token = default);
    }
}

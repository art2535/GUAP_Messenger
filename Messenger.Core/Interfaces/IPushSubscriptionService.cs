using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IPushSubscriptionService
    {
        Task<List<PushSubscription>> GetSubscriptionsByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task AddSubscriptionAsync(PushSubscription subscription, CancellationToken ct = default);
        Task RemoveSubscriptionAsync(Guid id, CancellationToken ct = default);
        Task RemoveByEndpointAsync(string endpoint, CancellationToken ct = default);
        Task UpdateSubscriptionAsync(PushSubscription subscription, CancellationToken ct = default);
        Task SendPushToOfflineUsersAsync(Guid chatId, Guid senderId, string senderName, string? messageText, 
            bool hasAttachments, CancellationToken ct = default);
    }
}

using Messenger.Core.DTOs.Push;
using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IPushSubscriptionService
    {
        Task<List<PushSubscription>> GetSubscriptionsByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task AddSubscriptionAsync(PushSubscription subscription, CancellationToken ct = default);
        Task RemoveSubscriptionAsync(Guid id, CancellationToken ct = default);
        Task RemoveByEndpointAsync(string endpoint, CancellationToken ct = default);
        Task<AccountSetting?> GetPushSettingsAsync(Guid accountId, CancellationToken token = default);
        Task SavePushSettingsAsync(Guid userId, Guid accountId, PushSubscriptionUpdateRequest request,
            CancellationToken token = default);
        Task UpdateSubscriptionAsync(PushSubscription subscription, CancellationToken ct = default);
        Task SendPushToOfflineUsersAsync(Guid chatId, Guid senderId, string senderName, string? messageText, 
            bool hasAttachments, bool isMention = false, CancellationToken cancellationToken = default);
    }
}

using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IBroadcastRepository
    {
        Task<Broadcast> AddBroadcastAsync(Broadcast broadcast);
        Task AddRecipientsRangeAsync(IEnumerable<BroadcastRecipient> recipients);
        Task<Broadcast?> GetBroadcastByIdAsync(Guid id, bool asNoTracking = true);
        Task<List<Guid>> GetExistingUserIdsAsync(IEnumerable<Guid> userIds);
        Task<BroadcastRecipient?> GetRecipientAsync(Guid broadcastId, Guid userId);
        Task UpdateRecipientAsync(BroadcastRecipient recipient);
        Task SaveChangesAsync();
        Task<List<BroadcastRecipient>> GetRecipientStatusesAsync(Guid broadcastId);
        Task<List<(Broadcast Broadcast, bool IsRead, DateTime? ReadAt)>> GetUserBroadcastsAsync(
            Guid userId, bool unreadOnly);
        Task<(int ReadCount, DateTime? FirstRead)?> GetReadStatsAsync(Guid broadcastId);
    }
}

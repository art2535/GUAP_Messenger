using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class PushSubscriptionRepository
    {
        private readonly GuapMessengerContext _context;

        public PushSubscriptionRepository(GuapMessengerContext context)
        {
            _context = context;
        }

        public async Task<List<PushSubscription>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _context.PushSubscriptions
                .Where(s => s.UserId == userId)
                .ToListAsync(ct);
        }

        public async Task AddAsync(PushSubscription subscription, CancellationToken ct = default)
        {
            _context.PushSubscriptions.Add(subscription);
            await _context.SaveChangesAsync(ct);
        }

        public async Task RemoveAsync(Guid id, CancellationToken ct = default)
        {
            var sub = await _context.PushSubscriptions.FindAsync(new object[] { id }, ct);
            if (sub != null)
            {
                _context.PushSubscriptions.Remove(sub);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task UpdateAsync(PushSubscription subscription, CancellationToken ct = default)
        {
            _context.PushSubscriptions.Update(subscription);
            await _context.SaveChangesAsync(ct);
        }

        public async Task RemoveByEndpointAsync(string endpoint, CancellationToken ct = default)
        {
            var subscriptions = await _context.PushSubscriptions
                .Where(s => s.Endpoint == endpoint)
                .ToListAsync(ct);

            if (subscriptions.Any())
            {
                _context.PushSubscriptions.RemoveRange(subscriptions);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<List<PushSubscription>> GetByChatParticipantsAsync(Guid chatId, Guid senderId, 
            CancellationToken ct = default)
        {
            var participantIds = await _context.ChatParticipants
                .Where(cp => cp.ChatId == chatId && cp.UserId != senderId)
                .Select(cp => cp.UserId)
                .ToListAsync(ct);

            if (participantIds.Count == 0)
                return new List<PushSubscription>();

            return await _context.PushSubscriptions
                .Where(s => participantIds.Contains(s.UserId))
                .ToListAsync(ct);
        }

        public async Task UpdateLastUsedAsync(Guid subscriptionId, CancellationToken ct = default)
        {
            var sub = await _context.PushSubscriptions.FindAsync(new object[] { subscriptionId }, ct);
            if (sub != null)
            {
                sub.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
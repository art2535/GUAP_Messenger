using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class BroadcastRepository
    {
        private readonly GuapMessengerContext _context;

        public BroadcastRepository(GuapMessengerContext context)
        {
            _context = context;
        }

        public async Task<Broadcast> AddBroadcastAsync(Broadcast broadcast)
        {
            _context.Broadcasts.Add(broadcast);
            await _context.SaveChangesAsync();
            return broadcast;
        }

        public async Task AddRecipientsRangeAsync(IEnumerable<BroadcastRecipient> recipients)
        {
            _context.BroadcastRecipients.AddRange(recipients);
            await _context.SaveChangesAsync();
        }

        public async Task<Broadcast?> GetBroadcastByIdAsync(Guid id, bool asNoTracking = true)
        {
            var query = _context.Broadcasts.AsQueryable();
            if (asNoTracking)
                query = query.AsNoTracking();
            return await query.FirstOrDefaultAsync(b => b.BroadcastId == id);
        }

        public async Task<List<BroadcastRecipient>> GetRecipientStatusesAsync(Guid broadcastId)
        {
            return await _context.BroadcastRecipients
                .AsNoTracking()
                .Where(r => r.BroadcastId == broadcastId)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetExistingUserIdsAsync(IEnumerable<Guid> userIds)
        {
            return await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => u.UserId)
                .ToListAsync();
        }

        public async Task<BroadcastRecipient?> GetRecipientAsync(Guid broadcastId, Guid userId)
        {
            return await _context.BroadcastRecipients
                .FirstOrDefaultAsync(r => r.BroadcastId == broadcastId && r.UserId == userId);
        }

        public async Task UpdateRecipientAsync(BroadcastRecipient recipient)
        {
            _context.BroadcastRecipients.Update(recipient);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<(Broadcast Broadcast, bool IsRead, DateTime? ReadAt)>> GetUserBroadcastsAsync(
            Guid userId, bool unreadOnly)
        {
            var query = _context.BroadcastRecipients
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Include(r => r.Broadcast)
                .AsQueryable();

            if (unreadOnly)
                query = query.Where(r => !r.IsRead);

            var result = await query
                .OrderByDescending(r => r.Broadcast.CreatedAt)
                .ToListAsync();

            return result.Select(r => (r.Broadcast, r.IsRead, r.ReadAt)).ToList();
        }

        public async Task<(int ReadCount, DateTime? FirstRead)?> GetReadStatsAsync(Guid broadcastId)
        {
            var stats = await _context.BroadcastRecipients
                .Where(r => r.BroadcastId == broadcastId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    ReadCount = g.Count(r => r.IsRead),
                    FirstRead = g.Min(r => r.ReadAt)
                })
                .FirstOrDefaultAsync();

            if (stats == null)
                return null;
            return (stats.ReadCount, stats.FirstRead);
        }
    }
}

using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class NotificationRepository
    {
        private readonly GuapMessengerContext _context;

        public NotificationRepository(GuapMessengerContext context)
        {
            _context = context;
        }

        public async Task AddNotificationAsync(Notification notification, CancellationToken token = default)
        {
            await _context.Notifications.AddAsync(notification, token);
            await _context.SaveChangesAsync(token);
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(Guid userId, 
            CancellationToken token = default)
        {
            return await _context.Notifications
                .Where(notification => notification.UserId == userId)
                .ToListAsync(token);
        }
    }
}

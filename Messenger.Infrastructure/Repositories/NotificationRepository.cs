using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Messenger.Infrastructure.Repositories
{
    public class NotificationRepository
    {
        private readonly GuapMessengerContext _context;
        private readonly ILogger<NotificationRepository> _logger;

        public NotificationRepository(GuapMessengerContext context, ILogger<NotificationRepository> logger)
        {
            _context = context;
            _logger = logger;
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

        public async Task MarkAsReadAsync(Guid notificationId, CancellationToken token = default)
        {
            var updatedRows = await _context.Notifications
                .Where(n => n.NotificationId == notificationId)
                .ExecuteUpdateAsync(property => property
                    .SetProperty(n => n.Read, true)
                    .SetProperty(n => n.ReadAt, Notification.Now),
                token);

            if (updatedRows == 0)
            {
                _logger.LogWarning("Нет обновленных строк в таблице уведомлений");
            }
        }
    }
}

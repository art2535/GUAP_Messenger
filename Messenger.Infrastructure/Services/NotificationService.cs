using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;

namespace Messenger.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationRepository _repository;

        public NotificationService(NotificationRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateNotificationAsync(Guid userId, string text, CancellationToken token = default)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                Text = text,
                CreationDate = DateTime.UtcNow,
                Read = false
            };

            await _repository.AddNotificationAsync(notification, token);
        }

        public async Task<IEnumerable<Notification>> GetNotificationsAsync(Guid userId, CancellationToken token = default)
        {
            return await _repository.GetNotificationsByUserIdAsync(userId, token);
        }
    }
}

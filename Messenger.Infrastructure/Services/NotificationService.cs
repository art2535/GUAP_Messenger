using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;

namespace Messenger.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationRepository _repository;
        private readonly IEncryptionService _encryptionService;

        public NotificationService(NotificationRepository repository, IEncryptionService encryptionService)
        {
            _repository = repository;
            _encryptionService = encryptionService;
        }

        public async Task<Guid> CreateNotificationAsync(Guid userId, string text, CancellationToken token = default)
        {
            string encryptedText = _encryptionService.Encrypt(text);

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Text = encryptedText,
                CreationDate = Notification.Now,
                Read = false
            };

            await _repository.AddNotificationAsync(notification, token);
            return notification.NotificationId;
        }

        public async Task<IEnumerable<Notification>> GetNotificationsAsync(Guid userId, CancellationToken token = default)
        {
            var notifications = await _repository.GetNotificationsByUserIdAsync(userId, token);

            foreach (var notification in notifications)
            {
                notification.Text = _encryptionService.TryDecryptSafe(notification.Text);
            }

            return notifications;
        }
        
        public async Task MarkAsReadAsync(Guid notificationId, CancellationToken token = default)
        {
            await _repository.MarkAsReadAsync(notificationId, token);
        }

        public async Task<Notification?> GetNotificationAsync(Guid notificationId, CancellationToken token = default)
        {
            var notification = await _repository.GetNotificationByIdAsync(notificationId, token);
            if (notification != null)
            {
                notification.Text = _encryptionService.TryDecryptSafe(notification.Text);
            }
            return notification;
        }
    }
}

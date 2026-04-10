using Messenger.Core.Interfaces;
using Messenger.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebPush;
using PushSubscription = Messenger.Core.Models.PushSubscription;

namespace Messenger.Infrastructure.Services
{
    public class PushSubscriptionService : IPushSubscriptionService
    {
        private readonly PushSubscriptionRepository _repository;
        private readonly IChatService _chatService;
        private readonly INotificationService _notificationService;
        private readonly WebPushClient _webPushClient;
        private readonly VapidDetails _vapidDetails;
        private readonly ILogger<PushSubscriptionService> _logger;
        private readonly IEncryptionService _encryptionService;

        public PushSubscriptionService(PushSubscriptionRepository repository, IChatService chatService,
            INotificationService notificationService, WebPushClient webPushClient, IConfiguration configuration,
            ILogger<PushSubscriptionService> logger, IEncryptionService encryptionService)
        {
            _repository = repository;
            _chatService = chatService;
            _notificationService = notificationService;
            _webPushClient = webPushClient;
            _logger = logger;

            var vapidSection = configuration.GetSection("Vapid");
            _vapidDetails = new VapidDetails(vapidSection["Subject"], vapidSection["PublicKey"]!,
                vapidSection["PrivateKey"]!);
            _encryptionService = encryptionService;
        }

        public async Task<List<PushSubscription>> GetSubscriptionsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _repository.GetByUserIdAsync(userId, ct);
        }

        public async Task AddSubscriptionAsync(PushSubscription subscription, CancellationToken ct = default)
        {
            await _repository.AddAsync(subscription, ct);
        }

        public async Task RemoveSubscriptionAsync(Guid id, CancellationToken ct = default)
        {
            await _repository.RemoveAsync(id, ct);
        }

        public async Task UpdateSubscriptionAsync(PushSubscription subscription, CancellationToken ct = default)
        {
            await _repository.UpdateAsync(subscription, ct);
        }

        public async Task RemoveByEndpointAsync(string endpoint, CancellationToken ct = default)
        {
            await _repository.RemoveByEndpointAsync(endpoint, ct);
        }

        public async Task SendPushToOfflineUsersAsync(Guid chatId, Guid senderId, string senderName,
            string? messageText, bool hasAttachments, CancellationToken cancellationToken)
        {
            try
            {
                string body = "Новое сообщение";

                if (!string.IsNullOrWhiteSpace(messageText))
                {
                    body = messageText.Length > 60
                        ? messageText[..57] + "..."
                        : messageText;
                }
                else if (hasAttachments)
                {
                    body = "Прикрепил файл";
                }

                string notificationText = $"Новое сообщение от {senderName}: {body}";

                _logger.LogInformation("Отправка push от {Sender} в чат {ChatId}", senderName, chatId);

                var participants = await _chatService.GetChatParticipantsAsync(chatId, cancellationToken);

                foreach (var participant in participants.Where(p => p.UserId != senderId))
                {
                    string encryptedNotificationText = _encryptionService.Encrypt(notificationText);

                    var notificationId = await _notificationService.CreateNotificationAsync(
                        participant.UserId,
                        encryptedNotificationText,
                        cancellationToken);

                    var subscriptions = await _repository.GetByUserIdAsync(participant.UserId, cancellationToken);

                    foreach (var sub in subscriptions)
                    {
                        if (string.IsNullOrEmpty(sub.P256dh) || string.IsNullOrEmpty(sub.Auth))
                            continue;

                        try
                        {
                            var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);

                            var payload = new
                            {
                                title = "Новое сообщение",
                                body = body,
                                sender = senderName,
                                url = $"/Account/Chats?chatId={chatId}",
                                chatId = chatId.ToString(),
                                notificationId = notificationId.ToString()
                            };

                            var payloadJson = JsonSerializer.Serialize(payload);

                            await _webPushClient.SendNotificationAsync(
                                pushSubscription,
                                payloadJson,
                                _vapidDetails,
                                cancellationToken);

                            await _repository.UpdateLastUsedAsync(sub.Id, cancellationToken);
                        }
                        catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                        {
                            await _repository.RemoveAsync(sub.Id, cancellationToken);
                            _logger.LogInformation("Удалена устаревшая подписка для пользователя {UserId}", participant.UserId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка отправки push пользователю {UserId}", participant.UserId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при отправке push для чата {ChatId}", chatId);
            }
        }
    }
}

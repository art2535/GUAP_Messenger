using Messenger.Core.Interfaces;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebPush;
using PushSubscription = Messenger.Core.Models.PushSubscription;

namespace Messenger.Infrastructure.Services
{
    public class PushSubscriptionService : IPushSubscriptionService
    {
        private readonly GuapMessengerContext _context;
        private readonly IChatService _chatService;
        private readonly WebPushClient _webPushClient;
        private readonly VapidDetails _vapidDetails;
        private readonly ILogger<PushSubscriptionService> _logger;

        public PushSubscriptionService(GuapMessengerContext context, IChatService chatService, WebPushClient webPushClient,
            ILogger<PushSubscriptionService> logger, IConfiguration configuration)
        {
            _context = context;
            _chatService = chatService;
            _logger = logger;

            var vapidSection = configuration.GetSection("Vapid");
            _vapidDetails = new VapidDetails(
                subject: vapidSection["Subject"] ?? "mailto:admin@guap.ru",
                publicKey: vapidSection["PublicKey"]!,
                privateKey: vapidSection["PrivateKey"]!
            );

            _webPushClient = webPushClient;
        }

        public async Task<List<PushSubscription>> GetSubscriptionsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _context.PushSubscriptions
                .Where(s => s.UserId == userId)
                .ToListAsync(ct);
        }

        public async Task AddSubscriptionAsync(PushSubscription subscription, CancellationToken ct = default)
        {
            _context.PushSubscriptions.Add(subscription);
            await _context.SaveChangesAsync(ct);
        }

        public async Task RemoveSubscriptionAsync(Guid id, CancellationToken ct = default)
        {
            var sub = await _context.PushSubscriptions.FindAsync(new object[] { id }, ct);
            if (sub != null)
            {
                _context.PushSubscriptions.Remove(sub);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task UpdateSubscriptionAsync(PushSubscription subscription, CancellationToken ct = default)
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

        public async Task SendPushToOfflineUsersAsync(Guid chatId, Guid senderId, string senderName, string? messageText,
            bool hasAttachments, CancellationToken cancellationToken)
        {
            try
            {
                string body = "Новое сообщение";

                if (!string.IsNullOrWhiteSpace(messageText))
                {
                    body = messageText.Length > 120
                        ? messageText.Substring(0, 117) + "..."
                        : messageText;
                }
                else if (hasAttachments)
                {
                    body = "Прикрепил файл";
                }

                var payload = new
                {
                    title = "Новое сообщение",
                    body = body,
                    sender = senderName,
                    url = $"/Account/Chats?chatId={chatId}",
                    chatId = chatId.ToString()
                };

                var payloadJson = JsonSerializer.Serialize(payload);

                _logger.LogInformation("Отправка Telegram-style push от {Sender} в чат {ChatId}", senderName, chatId);

                var participants = await _chatService.GetChatParticipantsAsync(chatId, cancellationToken);

                foreach (var participant in participants.Where(p => p.UserId != senderId))
                {
                    var subscriptions = await _context.PushSubscriptions
                        .Where(s => s.UserId == participant.UserId)
                        .ToListAsync(cancellationToken);

                    foreach (var sub in subscriptions)
                    {
                        if (string.IsNullOrEmpty(sub.P256dh) || string.IsNullOrEmpty(sub.Auth))
                            continue;

                        try
                        {
                            var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);

                            await _webPushClient.SendNotificationAsync(
                                pushSubscription,
                                payloadJson,
                                _vapidDetails,
                                cancellationToken);

                            sub.LastUsedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                        catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                        {
                            _context.PushSubscriptions.Remove(sub);
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Не удалось отправить push пользователю {UserId}", participant.UserId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка в SendPushToOfflineUsersAsync для чата {ChatId}", chatId);
            }
        }
    }
}

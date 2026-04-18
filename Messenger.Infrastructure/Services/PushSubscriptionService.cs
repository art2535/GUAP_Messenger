using Messenger.Core.DTOs.Push;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;
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
        private readonly WebPushClient _webPushClient;
        private readonly VapidDetails _vapidDetails;
        private readonly ILogger<PushSubscriptionService> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        public PushSubscriptionService(PushSubscriptionRepository repository, IChatService chatService,
            WebPushClient webPushClient, IConfiguration configuration, INotificationService notificationService,
            ILogger<PushSubscriptionService> logger, IEncryptionService encryptionService, IUserService userService)
        {
            _repository = repository;
            _chatService = chatService;
            _webPushClient = webPushClient;
            _logger = logger;

            var vapidSection = configuration.GetSection("Vapid");
            _vapidDetails = new VapidDetails(vapidSection["Subject"], vapidSection["PublicKey"]!,
                vapidSection["PrivateKey"]!);
            _encryptionService = encryptionService;
            _userService = userService;
            _notificationService = notificationService;
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

        public async Task<AccountSetting?> GetPushSettingsAsync(Guid userId, CancellationToken token = default)
        {
            var user = await _userService.GetUserByIdAsync(userId, token);
            if (user?.AccountId == null)
                return null;

            return await _repository.GetAccountSettingsAsync(user.AccountId, token);
        }

        public async Task SavePushSettingsAsync(Guid userId, Guid accountId, PushSubscriptionUpdateRequest request, 
            CancellationToken token = default)
        {
            await _repository.SavePushSettingsAsync(accountId, request, token);

            if (!request.PushEnabled)
            {
                await _repository.RemoveAllSubscriptionsForUserAsync(userId, token);
                _logger.LogInformation("Все push-подписки удалены для пользователя {UserId}", userId);
            }
        }

        public async Task SendPushToOfflineUsersAsync(Guid chatId, Guid senderId, string senderName,
            string? messageText, bool hasAttachments, bool isMention = false, CancellationToken cancellationToken = default)
        {
            try
            {
                string body = "Новое сообщение";

                if (!string.IsNullOrWhiteSpace(messageText))
                {
                    string decrypted = _encryptionService.TryDecryptSafe(messageText);

                    if (!string.IsNullOrEmpty(decrypted) && !decrypted.StartsWith("Encrypted:"))
                    {
                        body = decrypted.Length > 60 ? decrypted[..57] + "..." : decrypted;
                    }
                    else
                    {
                        body = messageText.Length > 60 ? messageText[..57] + "..." : messageText;
                    }
                }
                else if (hasAttachments)
                {
                    body = "Прикрепил файл";
                }

                var chat = await _chatService.GetChatByIdAsync(chatId, cancellationToken);
                if (chat == null) 
                    return;

                bool isGroupChat = chat.Type?.ToLower() == "group";
                var participants = await _chatService.GetChatParticipantsAsync(chatId, cancellationToken);

                foreach (var participant in participants.Where(p => p.UserId != senderId))
                {
                    var settings = await GetPushSettingsAsync(participant.UserId, cancellationToken);

                    if (settings == null || !settings.PushEnabled)
                    {
                        _logger.LogWarning("Пропуск пользователя {UserId}: Push-уведомления полностью отключены", participant.UserId);
                        continue;
                    }

                    if (isGroupChat && !settings.NotifyGroupChats)
                    {
                        _logger.LogWarning("Пропуск пользователя {UserId}: Групповые уведомления отключены", participant.UserId);
                        continue;
                    }

                    if (!isGroupChat && !settings.NotifyMessages)
                    {
                        _logger.LogWarning("Пропуск пользователя {UserId}: Уведомления о сообщениях отключены", participant.UserId);
                        continue;
                    }

                    if (isMention && !settings.NotifyMentions)
                    {
                        _logger.LogWarning("Пропуск пользователя {UserId}: Уведомления об упоминаниях отключены", participant.UserId);
                        continue;
                    }

                    try
                    {
                        string notificationText = $"Новое сообщение от {senderName}: {body}";
                        var notificationId = await _notificationService.CreateNotificationAsync(participant.UserId,
                            _encryptionService.Encrypt(notificationText), cancellationToken);

                        var subscriptions = await _repository.GetByUserIdAsync(participant.UserId, cancellationToken);

                        if (!subscriptions.Any())
                        {
                            _logger.LogInformation("У пользователя {UserId} нет активных push-подписок", participant.UserId);
                            continue;
                        }

                        foreach (var sub in subscriptions)
                        {
                            if (string.IsNullOrEmpty(sub.P256dh) || string.IsNullOrEmpty(sub.Auth))
                                continue;

                            try
                            {
                                var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);

                                var payload = new
                                {
                                    title = isGroupChat ? "Новое сообщение в группе" : "Новое сообщение",
                                    body = body,
                                    sender = senderName,
                                    chatId = chatId.ToString(),
                                    notificationId = notificationId.ToString()
                                };

                                await _webPushClient.SendNotificationAsync(pushSubscription, JsonSerializer.Serialize(payload),
                                    _vapidDetails, cancellationToken);

                                await _repository.UpdateLastUsedAsync(sub.Id, cancellationToken);
                            }
                            catch (WebPushException webEx) when (webEx.Message.Contains("no longer valid") ||
                                                                 webEx.Message.Contains("unsubscribed") ||
                                                                 webEx.Message.Contains("expired"))
                            {
                                _logger.LogWarning("Подписка пользователя {UserId} устарела. Удаляем...", participant.UserId);
                                await _repository.RemoveByEndpointAsync(sub.Endpoint, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Ошибка отправки push пользователю {UserId}", participant.UserId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ошибка при создании уведомления или отправке push для пользователя {UserId}", participant.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Общая ошибка при отправке push для чата {ChatId}", chatId);
            }
        }
    }
}

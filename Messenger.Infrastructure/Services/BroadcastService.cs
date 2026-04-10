using Messenger.Core.DTOs.Broadcasts;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;

namespace Messenger.Infrastructure.Services
{
    public class BroadcastService : IBroadcastService
    {
        private readonly BroadcastRepository _repository;

        public BroadcastService(BroadcastRepository repository)
        {
            _repository = repository;
        }

        public async Task<BroadcastCreatedResponse> CreateBroadcastAsync(CreateBroadcastRequest request, Guid senderId)
        {
            var existingIds = await _repository.GetExistingUserIdsAsync(request.RecipientIds);

            if (existingIds.Count != request.RecipientIds.Count)
                throw new ArgumentException("Один или несколько получателей не существуют");

            var broadcast = new Broadcast
            {
                Title = request.Title,
                MessageText = request.MessageText,
                SenderId = senderId,
                CreatedAt = DateTime.UtcNow,
                TotalRecipients = existingIds.Count
            };

            await _repository.AddBroadcastAsync(broadcast);

            var recipients = existingIds.Select(uid => new BroadcastRecipient
            {
                BroadcastId = broadcast.BroadcastId,
                UserId = uid,
                SentAt = DateTime.UtcNow
            }).ToList();

            await _repository.AddRecipientsRangeAsync(recipients);

            return new BroadcastCreatedResponse
            {
                BroadcastId = broadcast.BroadcastId,
                TotalRecipients = broadcast.TotalRecipients,
                CreatedAt = broadcast.CreatedAt
            };
        }

        public async Task<BroadcastSummaryDto?> GetBroadcastSummaryAsync(Guid id, Guid currentUserId, bool isAdmin)
        {
            var broadcast = await _repository.GetBroadcastByIdAsync(id);
            if (broadcast == null)
                return null;

            if (broadcast.SenderId != currentUserId && !isAdmin)
                throw new UnauthorizedAccessException("Нет прав на просмотр этой рассылки");

            var recipients = await _repository.GetRecipientStatusesAsync(id);
            var stats = await _repository.GetReadStatsAsync(id);

            return new BroadcastSummaryDto
            {
                BroadcastId = broadcast.BroadcastId,
                Title = broadcast.Title,
                MessageText = broadcast.MessageText,
                SenderId = broadcast.SenderId,
                CreatedAt = broadcast.CreatedAt,
                TotalRecipients = broadcast.TotalRecipients,
                ReadCount = stats?.ReadCount ?? 0,
                Recipients = recipients.Select(r => new RecipientStatusDto
                {
                    UserId = r.UserId,
                    IsRead = r.IsRead,
                    ReadAt = r.ReadAt
                }).ToList()
            };
        }

        public async Task<MarkAsReadResponse> MarkAsReadAsync(Guid broadcastId, Guid userId)
        {
            var recipient = await _repository.GetRecipientAsync(broadcastId, userId);
            if (recipient == null)
                throw new KeyNotFoundException("Вы не являетесь получателем этой рассылки");

            if (recipient.IsRead)
            {
                return new MarkAsReadResponse
                {
                    Success = true,
                    ReadAt = recipient.ReadAt
                };
            }

            recipient.IsRead = true;
            recipient.ReadAt = DateTime.UtcNow;

            await _repository.UpdateRecipientAsync(recipient);

            return new MarkAsReadResponse
            {
                Success = true,
                ReadAt = recipient.ReadAt
            };
        }

        public async Task<List<object>> GetMyBroadcastsAsync(Guid userId, bool unreadOnly = true)
        {
            var items = await _repository.GetUserBroadcastsAsync(userId, unreadOnly);

            return items.Select(x => new
            {
                x.Broadcast.BroadcastId,
                x.Broadcast.Title,
                x.Broadcast.MessageText,
                x.Broadcast.CreatedAt,
                IsRead = x.IsRead,
                ReadAt = x.ReadAt
            }).ToList<object>();
        }
    }
}

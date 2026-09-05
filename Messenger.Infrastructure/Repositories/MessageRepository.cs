using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class MessageRepository
    {
        private readonly GuapMessengerContext _context;

        public MessageRepository(GuapMessengerContext context)
        {
            _context = context;
        }

        public async Task AddMessageAsync(Message message, CancellationToken token = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var lockKey = BitConverter.ToInt64(message.ChatId.ToByteArray(), 0);
                await _context.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock({0})",
                    new object[] { lockKey },
                    token);

                var lastSeq = await _context.Messages
                    .Where(m => m.ChatId == message.ChatId)
                    .MaxAsync(m => (long?)m.SequenceNumber, token) ?? 0L;

                message.SequenceNumber = lastSeq + 1;
                message.DeliveryStatus = MessageDeliveryStatus.Pending;

                await _context.Messages.AddAsync(message, token);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
            }
            catch
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }

        public async Task<(IReadOnlyList<Message> Items, bool HasMore)> GetMessagesByChatIdPagedAsync(Guid chatId,
            long? beforeSequence = null, int limit = 50, CancellationToken token = default)
        {
            if (limit <= 0)
                limit = 50;
            if (limit > 200)
                limit = 200;

            var query = _context.Messages
                .AsNoTracking()
                .Where(m => m.ChatId == chatId);

            if (beforeSequence.HasValue)
                query = query.Where(m => m.SequenceNumber < beforeSequence.Value);

            var batch = await query
                .OrderByDescending(m => m.SequenceNumber)
                .Take(limit + 1)
                .Include(m => m.Sender)
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .Include(m => m.Attachments)
                .AsSplitQuery()
                .ToListAsync(token);

            var hasMore = batch.Count > limit;
            if (hasMore)
                batch = batch.Take(limit).ToList();

            batch.Reverse();
            return (batch, hasMore);
        }

        [Obsolete("Use GetMessagesByChatIdPagedAsync for production load")]
        public async Task<IEnumerable<Message>> GetMessagesByChatIdAsync(Guid chatId, CancellationToken token = default)
        {
            var (items, _) = await GetMessagesByChatIdPagedAsync(chatId, beforeSequence: null, limit: 500, token);
            return items;
        }

        public async Task<Message?> GetMessageByIdAsync(Guid chatId, Guid messageId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Messages
                .FirstOrDefaultAsync(m => m.ChatId == chatId && m.MessageId == messageId, cancellationToken);
        }

        public async Task UpdateMessageAsync(Message message, CancellationToken token = default)
        {
            _context.Messages.Update(message);
            await _context.SaveChangesAsync(token);
        }

        public async Task DeleteMessageAsync(Guid messageId, CancellationToken token = default)
        {
            var deletedMessage = await _context.Messages.FindAsync(new object[] { messageId }, token);
            if (deletedMessage != null)
            {
                _context.Messages.Remove(deletedMessage);
                await _context.SaveChangesAsync(token);
            }
        }
    }
}

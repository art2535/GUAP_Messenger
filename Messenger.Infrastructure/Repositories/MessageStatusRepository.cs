using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class MessageStatusRepository
    {
        private readonly GuapMessengerContext _context;

        public MessageStatusRepository(GuapMessengerContext context)
        {
            _context = context;
        }

        public async Task AddOrUpdateMessageStatusAsync(MessageStatus messageStatus, 
            CancellationToken cancellationToken = default)
        {
            var existing = await _context.MessageStatuses
                .FirstOrDefaultAsync(ms => ms.MessageId == messageStatus.MessageId 
                && ms.UserId == messageStatus.UserId, cancellationToken);

            if (existing != null)
            {
                existing.Status = messageStatus.Status;
                existing.ChangeDate = DateTime.UtcNow;
                _context.MessageStatuses.Update(existing);
            }
            else
            {
                await _context.MessageStatuses.AddAsync(messageStatus, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<MessageStatus>> GetMessageStatusByMessageIdAsync(Guid messageId, 
            CancellationToken cancellationToken = default)
        {
            return await _context.MessageStatuses
                .Where(ms => ms.MessageId == messageId)
                .Include(ms => ms.User)
                .ToListAsync(cancellationToken);
        }
    }
}

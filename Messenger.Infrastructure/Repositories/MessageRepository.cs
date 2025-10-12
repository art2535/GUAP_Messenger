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
            await _context.Messages.AddAsync(message, token);
            await _context.SaveChangesAsync(token);
        }

        public async Task<IEnumerable<Message>> GetMessagesByChatIdAsync(Guid chatId, CancellationToken token = default)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .ToListAsync(token);
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
            var deletedMessage = await _context.Messages.FindAsync(messageId);
            if (deletedMessage != null)
            {
                _context.Messages.Remove(deletedMessage);
                await _context.SaveChangesAsync(token);
            }
        }
    }
}

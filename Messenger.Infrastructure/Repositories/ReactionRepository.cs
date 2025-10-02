using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class ReactionRepository
    {
        private readonly GuapMessengerContext _context;

        public ReactionRepository(GuapMessengerContext context)
        {
            _context = context;
        }

        public async Task AddReactionAsync(Reaction reaction, CancellationToken cancellationToken = default)
        {
            await _context.Reactions.AddAsync(reaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<Reaction>> GetByMessageIdAsync(Guid messageId, 
            CancellationToken cancellationToken = default)
        {
            return await _context.Reactions
                .Where(r => r.MessageId == messageId)
                .Include(r => r.User)
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteReactionAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            var reaction = await _context.Reactions.FindAsync(messageId, cancellationToken);
            if (reaction != null)
            {
                _context.Reactions.Remove(reaction);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;

namespace Messenger.Infrastructure.Services
{
    public class ReactionService : IReactionService
    {
        private readonly ReactionRepository _reactionRepository;

        public ReactionService(ReactionRepository reactionRepository)
        {
            _reactionRepository = reactionRepository;
        }

        public async Task AddReactionAsync(Reaction reaction, CancellationToken cancellationToken = default)
        {
            await _reactionRepository.AddReactionAsync(reaction, cancellationToken);
        }

        public async Task<IEnumerable<Reaction>> GetReactionsByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            return await _reactionRepository.GetByMessageIdAsync(messageId, cancellationToken);
        }
    }
}

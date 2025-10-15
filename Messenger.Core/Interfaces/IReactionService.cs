using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IReactionService
    {
        Task AddReactionAsync(Reaction reaction, CancellationToken cancellationToken = default);
        Task<IEnumerable<Reaction>> GetReactionsByMessageIdAsync(Guid messageId, 
            CancellationToken cancellationToken = default);
        Task DeleteReactionAsync(Guid messageId, CancellationToken cancellationToken = default);
    }
}

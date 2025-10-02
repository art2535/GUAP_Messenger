using Messenger.Core.Models;

namespace Messenger.Core.Interfaces
{
    public interface IAttachmentService
    {
        Task AddAttachmentAsync(Attachment attachment, CancellationToken cancellationToken = default);
        Task<IEnumerable<Attachment>> GetAttachmentsByMessageIdAsync(Guid messageId, CancellationToken 
            cancellationToken = default);
    }
}

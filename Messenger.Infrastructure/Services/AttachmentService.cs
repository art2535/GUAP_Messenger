using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;

namespace Messenger.Infrastructure.Services
{
    public class AttachmentService : IAttachmentService
    {
        private AttachmentRepository _repository;

        public AttachmentService(AttachmentRepository repository)
        {
            _repository = repository;
        }

        public async Task AddAttachmentAsync(Attachment attachment, CancellationToken cancellationToken = default)
        {
            await _repository.AddAttachmentAsync(attachment, cancellationToken);
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentsByMessageIdAsync(Guid messageId, 
            CancellationToken cancellationToken = default)
        {
            return await _repository.GetByMessageIdAsync(messageId, cancellationToken);
        }
    }
}

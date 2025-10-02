using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Repositories
{
    public class AttachmentRepository
    {
        private readonly GuapMessengerContext _context;

        public AttachmentRepository(GuapMessengerContext context)
        {
            _context = context;
        }

        public async Task AddAttachmentAsync(Attachment attachment, CancellationToken cancellationToken = default)
        {
            await _context.Attachments.AddAsync(attachment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<Attachment>> GetByMessageIdAsync(Guid messageId, 
            CancellationToken cancellationToken = default)
        {
            return await _context.Attachments
                .Where(a => a.MessageId == messageId)
                .ToListAsync(cancellationToken);
        }
    }
}

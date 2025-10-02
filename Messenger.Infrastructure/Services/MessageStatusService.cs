using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;

namespace Messenger.Infrastructure.Services
{
    public class MessageStatusService : IMessageStatusService
    {
        private readonly MessageStatusRepository _messageStatusRepository;

        public MessageStatusService(MessageStatusRepository messageStatusRepository)
        {
            _messageStatusRepository = messageStatusRepository;
        }

        public async Task AddOrUpdateStatusAsync(MessageStatus messageStatus, CancellationToken cancellationToken = default)
        {
            await _messageStatusRepository.AddOrUpdateMessageStatusAsync(messageStatus, cancellationToken);
        }

        public async Task<IEnumerable<MessageStatus>> GetStatusesByMessageIdAsync(Guid messageId, 
            CancellationToken cancellationToken = default)
        {
            return await _messageStatusRepository.GetMessageStatusByMessageIdAsync(messageId, cancellationToken);
        }
    }
}

using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;

namespace Messenger.Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessageRepository _repository;

        public MessageService(MessageRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(Guid chatId, CancellationToken token = default)
        {
            return await _repository.GetMessagesByChatIdAsync(chatId, token);
        }

        public async Task SendMessageAsync(Guid chatId, Guid senderId, Guid receiverId, string content, 
            bool hasAttachments, CancellationToken token = default)
        {
            var message = new Message
            {
                MessageId = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                RecipientId = receiverId,
                MessageText = content,
                HasAttachments = hasAttachments,
                SendTime = DateTime.UtcNow
            };

            await _repository.AddMessageAsync(message, token);
        }

        public async Task<Message?> GetMessageByIdAsync(Guid chatId, Guid messageId, CancellationToken token = default)
        {
            return await _repository.GetMessageByIdAsync(chatId, messageId, token);
        }

        public async Task DeleteMessageAsync(Guid messageId, CancellationToken token = default)
        {
            await _repository.DeleteMessageAsync(messageId, token);
        }

        public async Task UpdateMessageAsync(Message message, CancellationToken token = default)
        {
            await _repository.UpdateMessageAsync(message, token);
        }
    }
}

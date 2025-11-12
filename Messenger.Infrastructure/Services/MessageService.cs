using Messenger.Core.Hubs;
using Messenger.Core.DTOs;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Messenger.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace Messenger.Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessageRepository _repository;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessageService(MessageRepository repository, IHubContext<ChatHub> hubContext)
        {
            _repository = repository;
            _hubContext = hubContext;
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(Guid chatId, CancellationToken token = default)
        {
            return await _repository.GetMessagesByChatIdAsync(chatId, token);
        }

        public async Task<ServiceResult<Message>> SendMessageAsync(Guid chatId, Guid senderId, Guid? receiverId,
            string? content, bool hasAttachments, CancellationToken token = default)
        {
            try
            {
                var message = new Message
                {
                    MessageId = Guid.NewGuid(),
                    ChatId = chatId,
                    SenderId = senderId,
                    RecipientId = receiverId,
                    MessageText = content ?? string.Empty,
                    HasAttachments = hasAttachments,
                    SendTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };

                await _repository.AddMessageAsync(message, token);

                var loadedMessage = await _repository.GetMessageByIdAsync(chatId, message.MessageId, token);
                if (loadedMessage == null)
                    return ServiceResult<Message>.Failure("Не удалось загрузить сообщение");

                await _hubContext.Clients.Group($"Chat_{chatId}")
                    .SendAsync("ReceiveMessage", loadedMessage);

                return ServiceResult<Message>.Success(loadedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<Message>.Failure(ex.Message, ex.InnerException?.Message);
            }
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

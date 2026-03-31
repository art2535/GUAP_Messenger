using MassTransit;
using Messenger.Core.DTOs;
using Messenger.Core.DTOs.Messages;
using Messenger.Core.Interfaces;
using Messenger.Core.Messages;
using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Messenger.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;

namespace Messenger.Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessageRepository _repository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly GuapMessengerContext _context;
        private readonly IEncryptionService _encryptionService;

        public MessageService(MessageRepository repository, IPublishEndpoint publishEndpoint,
            GuapMessengerContext context, IEncryptionService encryptionService)
        {
            _repository = repository;
            _publishEndpoint = publishEndpoint;
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<List<MessageDto>> SearchMessagesAsync(Guid chatId, string query, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<MessageDto>();

            query = query.Trim().ToLowerInvariant();

            var messages = await _repository.GetMessagesByChatIdAsync(chatId, token);

            var filtered = new List<MessageDto>();

            foreach (var m in messages)
            {
                string decryptedText = _encryptionService.TryDecryptSafe(m.MessageText);

                if (decryptedText.ToLowerInvariant().Contains(query))
                {
                    filtered.Add(new MessageDto
                    {
                        MessageId = m.MessageId,
                        ChatId = m.ChatId,
                        SenderId = m.SenderId,
                        SenderName = m.Sender != null
                            ? $"{m.Sender.FirstName} {m.Sender.LastName}".Trim()
                            : "Пользователь",
                        MessageText = decryptedText,
                        SentAt = m.SendTime,
                        Status = m.DeliveryStatus.ToString(),
                        Attachments = m.Attachments?.Select(a => new AttachmentDto
                        {
                            AttachmentId = a.AttachmentId,
                            FileName = a.FileName,
                            FileType = a.FileType,
                            SizeInBytes = a.SizeInBytes ?? 0,
                            Url = a.Url
                        }).ToList() ?? new List<AttachmentDto>()
                    });
                }
            }

            return filtered.OrderBy(m => m.SentAt).ToList();
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(Guid chatId, CancellationToken token = default)
        {
            return await _repository.GetMessagesByChatIdAsync(chatId, token);
        }

        public async Task<ServiceResult<Message>> SendMessageAsync(Guid chatId, Guid senderId,
            string? content, bool hasAttachments, IFormFile[]? files = null, CancellationToken token = default)
        {
            try
            {
                var message = new Message
                {
                    MessageId = Guid.NewGuid(),
                    ChatId = chatId,
                    SenderId = senderId,
                    MessageText = content ?? string.Empty,
                    HasAttachments = hasAttachments,
                    SendTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };

                await _repository.AddMessageAsync(message, token);
                var sentMessage = await _repository.GetMessageByIdAsync(chatId, message.MessageId, token);

                await _publishEndpoint.Publish(new ChatMessageSent
                {
                    MessageId = sentMessage.MessageId,
                    ChatId = sentMessage.ChatId,
                    SequenceNumber = sentMessage.SequenceNumber,
                    SenderId = sentMessage.SenderId,
                    MessageText = sentMessage.MessageText,
                    SentAt = sentMessage.SendTime,
                    HasAttachments = sentMessage.HasAttachments
                }, token);

                sentMessage.DeliveryStatus = MessageDeliveryStatus.Sent;
                await _context.SaveChangesAsync(token);

                return sentMessage != null
                    ? ServiceResult<Message>.Success(sentMessage)
                    : ServiceResult<Message>.Failure("Не удалось загрузить сообщение");
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

using MassTransit;
using Messenger.Core.DTOs.Messages;
using Messenger.Core.Hubs;
using Messenger.Core.Interfaces;
using Messenger.Core.Messages;
using Messenger.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace Messenger.API.Consumers
{
    public class ChatMessageSentConsumer : IConsumer<ChatMessageSent>
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IMessageService _messageService;
        private readonly IAttachmentService _attachmentService;
        private readonly IEncryptionService _encryptionService;
        private readonly IMessageStatusService _messageStatusService;
        private readonly ILogger<ChatMessageSentConsumer> _logger;

        public ChatMessageSentConsumer(IHubContext<ChatHub> hubContext, IMessageService messageService,
            IAttachmentService attachmentService, IEncryptionService encryptionService, 
            ILogger<ChatMessageSentConsumer> logger, IMessageStatusService messageStatusService)
        {
            _hubContext = hubContext;
            _messageService = messageService;
            _attachmentService = attachmentService;
            _encryptionService = encryptionService;
            _logger = logger;
            _messageStatusService = messageStatusService;
        }

        public async Task Consume(ConsumeContext<ChatMessageSent> context)
        {
            var msg = context.Message;

            try
            {
                await _hubContext.Clients.Group(msg.ChatId.ToString())
                    .SendAsync("MessageSendingStatus", new
                    {
                        MessageId = msg.MessageId,
                        ChatId = msg.ChatId,
                        Status = "Processing",
                        Timestamp = DateTimeOffset.UtcNow
                    }, context.CancellationToken);

                var serviceResult = await _messageService.SendMessageAsync(
                    chatId: msg.ChatId,
                    senderId: msg.SenderId,
                    content: msg.MessageText,
                    hasAttachments: msg.HasAttachments,
                    files: null,
                    token: context.CancellationToken
                );

                if (!serviceResult.isSuccess || serviceResult.data == null)
                    throw new Exception($"Не удалось сохранить сообщение: {serviceResult.error}");

                var savedMessage = serviceResult.data;

                if (msg.Attachments?.Any() == true)
                {
                    foreach (var attInfo in msg.Attachments)
                    {
                        var attachment = new Attachment
                        {
                            AttachmentId = attInfo.AttachmentId,
                            MessageId = savedMessage.MessageId,
                            FileName = attInfo.FileName,
                            FileType = attInfo.FileType,
                            SizeInBytes = (int)attInfo.SizeInBytes,
                            Url = attInfo.Url
                        };
                        await _attachmentService.AddAttachmentAsync(attachment, context.CancellationToken);
                    }
                }

                savedMessage.DeliveryStatus = MessageDeliveryStatus.Delivered;
                await _messageService.UpdateMessageAsync(savedMessage, context.CancellationToken);

                var decryptedText = string.IsNullOrEmpty(msg.MessageText)
                    ? null
                    : _encryptionService.TryDecryptSafe(msg.MessageText);

                var finalMessageDto = new MessageDto
                {
                    MessageId = savedMessage.MessageId,
                    ChatId = savedMessage.ChatId,
                    SenderId = savedMessage.SenderId,
                    SenderName = msg.SenderName ?? "Пользователь",
                    MessageText = decryptedText,
                    SentAt = savedMessage.SendTime,
                    Status = "Sent",
                    Attachments = msg.Attachments?.Select(a => new AttachmentDto
                    {
                        AttachmentId = a.AttachmentId,
                        FileName = a.FileName,
                        FileType = a.FileType,
                        SizeInBytes = (int)a.SizeInBytes,
                        Url = a.Url
                    }).ToList() ?? new List<AttachmentDto>()
                };

                await _hubContext.Clients.Group(msg.ChatId.ToString())
                    .SendAsync("ReceiveMessage", finalMessageDto, context.CancellationToken);

                await _hubContext.Clients.Group(msg.ChatId.ToString())
                    .SendAsync("MessageSendingStatus", new MessageSendingStatus
                    {
                        MessageId = msg.MessageId,
                        ChatId = msg.ChatId,
                        Status = "Sent",
                        Timestamp = DateTimeOffset.UtcNow
                    }, context.CancellationToken);

                _logger.LogInformation("Сообщение {MessageId} полностью обработано", msg.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки сообщения {MessageId}", msg.MessageId);

                try
                {
                    var failedMessage = await _messageService.GetMessageByIdAsync(
                        msg.ChatId, msg.MessageId, context.CancellationToken);

                    if (failedMessage != null)
                    {
                        failedMessage.DeliveryStatus = MessageDeliveryStatus.Failed;
                        await _messageService.UpdateMessageAsync(failedMessage, context.CancellationToken);
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Не удалось обновить статус Failed");
                }

                await _hubContext.Clients.Group(msg.ChatId.ToString())
                    .SendAsync("MessageSendingStatus", new
                    {
                        MessageId = msg.MessageId,
                        ChatId = msg.ChatId,
                        Status = "Failed",
                        Reason = "Ошибка обработки сообщения",
                        Timestamp = DateTimeOffset.UtcNow
                    }, context.CancellationToken);

                throw;
            }
        }
    }
}

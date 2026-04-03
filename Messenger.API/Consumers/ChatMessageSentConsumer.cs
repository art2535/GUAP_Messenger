using MassTransit;
using Messenger.Core.Hubs;
using Messenger.Core.Messages;
using Messenger.Core.Models;
using Messenger.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Messenger.API.Consumers
{
    public class ChatMessageSentConsumer : IConsumer<ChatMessageSent>
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly GuapMessengerContext _dbContext;
        private readonly ILogger<ChatMessageSentConsumer> _logger;

        public ChatMessageSentConsumer(IHubContext<ChatHub> hubContext, GuapMessengerContext dbContext,
            ILogger<ChatMessageSentConsumer> logger)
        {
            _hubContext = hubContext;
            _dbContext = dbContext;
            _logger = logger;
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
                    });

                await _hubContext.Clients.Group(msg.ChatId.ToString())
                    .SendAsync("ReceiveMessage", msg, context.CancellationToken);

                var dbMessage = await _dbContext.Messages
                    .FirstOrDefaultAsync(m => m.MessageId == msg.MessageId, context.CancellationToken);

                if (dbMessage != null)
                {
                    dbMessage.DeliveryStatus = MessageDeliveryStatus.Delivered;
                    await _dbContext.SaveChangesAsync(context.CancellationToken);
                }

                await _hubContext.Clients.Group(msg.ChatId.ToString())
                    .SendAsync("MessageSendingStatus", new MessageSendingStatus
                    {
                        MessageId = msg.MessageId,
                        ChatId = msg.ChatId,
                        Status = "Sent",
                        Timestamp = DateTimeOffset.UtcNow
                    });

                _logger.LogInformation("Сообщение {MessageId} успешно обработано", msg.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки сообщения {MessageId}", msg.MessageId);

                try
                {
                    var tracked = await _dbContext.Messages.FindAsync([msg.MessageId], context.CancellationToken);
                    if (tracked != null)
                    {
                        tracked.DeliveryStatus = MessageDeliveryStatus.Failed;
                        await _dbContext.SaveChangesAsync(context.CancellationToken);
                    }
                }
                catch (Exception innnerEx)
                {
                    _logger.LogError(innnerEx, "Ошибка обновления статуса доставки сообщения {MessageId}", msg.MessageId);
                }

                await _hubContext.Clients.Group(msg.ChatId.ToString())
                    .SendAsync("MessageSendingStatus", new
                    {
                        MessageId = msg.MessageId,
                        ChatId = msg.ChatId,
                        Status = "Failed",
                        Reason = "Ошибка обработки сообщения",
                        Timestamp = DateTimeOffset.UtcNow
                    });

                throw;
            }
        }
    }
}

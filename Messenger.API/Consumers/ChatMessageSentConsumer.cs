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
                await _hubContext.Clients.Group($"chat-{msg.ChatId}")
                    .SendAsync("ReceiveMessage", msg, context.CancellationToken);

                var dbMessage = await _dbContext.Messages
                    .FirstOrDefaultAsync(m => m.MessageId == msg.MessageId);

                if (dbMessage != null)
                {
                    dbMessage.DeliveryStatus = MessageDeliveryStatus.Delivered;
                    await _dbContext.SaveChangesAsync(context.CancellationToken);
                }

                _logger.LogInformation("Сообщение {MessageId} (Seq {Seq}) доставлено в чат {ChatId}",
                    msg.MessageId, msg.SequenceNumber, msg.ChatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки сообщения {MessageId}", msg.MessageId);

                try
                {
                    var tracked = await _dbContext.Messages.FindAsync(msg.MessageId);
                    if (tracked != null)
                    {
                        tracked.DeliveryStatus = MessageDeliveryStatus.Failed;
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogWarning(innerEx, "Не удалось обновить статус Failed для сообщения {MessageId}", msg.MessageId);
                }

                throw;
            }
        }
    }
}

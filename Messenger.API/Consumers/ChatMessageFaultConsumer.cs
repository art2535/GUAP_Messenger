using MassTransit;
using Messenger.Core.Hubs;
using Messenger.Core.Messages;
using Microsoft.AspNetCore.SignalR;

namespace Messenger.API.Consumers
{
    public class ChatMessageFaultConsumer : IConsumer<Fault<ChatMessageSent>>
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatMessageFaultConsumer> _logger;

        public ChatMessageFaultConsumer(IHubContext<ChatHub> hubContext, ILogger<ChatMessageFaultConsumer> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<Fault<ChatMessageSent>> context)
        {
            var fault = context.Message;
            var original = fault.Message;

            _logger.LogError("Сообщение {MessageId} из чата {ChatId} попало в Error Queue. Исключений: {Count}",
                original.MessageId, original.ChatId, fault.Exceptions.Length);

            await _hubContext.Clients.Group($"chat-{original.ChatId}")
                .SendAsync("MessageDeliveryFailed", new
                {
                    messageId = original.MessageId,
                    chatId = original.ChatId,
                    reason = "Ошибка обработки на сервере"
                });
        }
    }
}

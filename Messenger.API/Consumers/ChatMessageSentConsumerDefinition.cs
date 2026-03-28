using MassTransit;

namespace Messenger.API.Consumers
{
    public class ChatMessageSentConsumerDefinition : ConsumerDefinition<ChatMessageSentConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ChatMessageSentConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rmq)
            {
                rmq.SetQuorumQueue(1);
            }

            endpointConfigurator.UseMessageRetry(r =>
                r.Incremental(6, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)));

            endpointConfigurator.UseConcurrencyLimit(20);
        }
    }
}

using MassTransit;
using Messenger.API.Consumers;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.API.Extensions
{
    public static class RabbitExtension
    {
        public static void AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddConsumer<ChatMessageSentConsumer>();

                x.AddConfigureEndpointsCallback((name, cfg) =>
                {
                    if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
                    {
                        rmq.SetQuorumQueue(1);
                    }
                });

                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitConfig = configuration.GetSection("RabbitMQ");

                    cfg.Host(rabbitConfig["Host"], ushort.Parse(rabbitConfig["Port"]), "/", h =>
                    {
                        h.Username(rabbitConfig["Username"]);
                        h.Password(rabbitConfig["Password"]);
                    });

                    cfg.UseMessageRetry(r =>
                    {
                        r.Incremental(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
                    });

                    cfg.UseConcurrencyLimit(30);
                    cfg.PrefetchCount = 15;

                    cfg.ConfigureEndpoints(context);
                });
            });

            services.AddMassTransitHostedService();
        }
    }
}
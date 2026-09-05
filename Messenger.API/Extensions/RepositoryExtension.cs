using Messenger.Infrastructure.Repositories;

namespace Messenger.API.Extensions
{
    public static class RepositoryExtension
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddRepositories()
            {
                services.AddScoped<UserRepository>();
                services.AddScoped<MessageRepository>();
                services.AddScoped<ChatRepository>();
                services.AddScoped<NotificationRepository>();
                services.AddScoped<AttachmentRepository>();
                services.AddScoped<ReactionRepository>();
                services.AddScoped<LoginRepository>();
                services.AddScoped<UserStatusRepository>();
                services.AddScoped<BroadcastRepository>();
                services.AddScoped<PushSubscriptionRepository>();

                return services;
            }
        }
    }
}

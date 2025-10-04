using Messenger.Infrastructure.Repositories;

namespace Messenger.API.Extensions
{
    public static class RepositoryExtension
    {
        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<UserRepository>();
            services.AddScoped<MessageRepository>();
            services.AddScoped<ChatRepository>();
            services.AddScoped<NotificationRepository>();
            services.AddScoped<AttachmentRepository>();
            services.AddScoped<ReactionRepository>();
            services.AddScoped<LoginRepository>();
            services.AddScoped<UserStatusRepository>();
            services.AddScoped<MessageStatusRepository>();
        }
    }
}

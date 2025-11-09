using Messenger.API.Providers;
using Messenger.Core.Interfaces;
using Messenger.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;

namespace Messenger.API.Extensions
{
    public static class ServiceExtension
    {
        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAttachmentService, AttachmentService>();
            services.AddScoped<IReactionService, ReactionService>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<IUserStatusService, UserStatusService>();
            services.AddScoped<IMessageStatusService, MessageStatusService>();
        }

        public static void AddSignalRService(this IServiceCollection services)
        {
            services.AddSignalR();
            services.AddSingleton<IUserIdProvider, NameUserIdProvider>();
        }
    }
}

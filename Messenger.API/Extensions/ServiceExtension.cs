using Messenger.API.Providers;
using Messenger.Core.Interfaces;
using Messenger.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using WebPush;

namespace Messenger.API.Extensions
{
    public static class ServiceExtension
    {
        extension(IServiceCollection services)
        {
            public void AddServices()
            {
                services.AddScoped<IUserService, UserService>();
                services.AddScoped<IChatService, ChatService>();
                services.AddScoped<INotificationService, NotificationService>();
                services.AddScoped<IAttachmentService, AttachmentService>();
                services.AddScoped<IReactionService, ReactionService>();
                services.AddScoped<ILoginService, LoginService>();
                services.AddScoped<IUserStatusService, UserStatusService>();
                services.AddScoped<IBroadcastService, BroadcastService>();
                services.AddScoped<IMessageService, MessageService>();
                services.AddSingleton<WebPushClient>();
                services.AddScoped<IPushSubscriptionService, PushSubscriptionService>();
            }

            public void AddSignalRService()
            {
                services.AddSignalR();
                services.AddSingleton<IUserIdProvider, NameUserIdProvider>();
            }

            public void AddEncryption(IConfiguration configuration)
            {
                services.Configure<AesGcmEncryptionOptions>(configuration.GetSection("Encryption"));
                services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();
            }
        }
    }
}

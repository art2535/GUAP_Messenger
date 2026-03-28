using Messenger.API.Extensions;
using Messenger.Core.Hubs;

namespace Messenger.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.EnsureSharedDevelopmentEncryptionKey();
            builder.Services.AddControllers();
            builder.Services.AddSignalRService();
            builder.Services.AddSwagger();
            builder.Services.AddPostgreSQL(builder.Configuration);
            builder.Services.AddRepositories();
            builder.Services.AddServices();
            builder.Services.AddEncryption(builder.Configuration);
            builder.Services.AddLogging();
            builder.Services.AddRabbitMQ(builder.Configuration);
            builder.Services.AddHttpClient();
            builder.Services.AddEtaApiAuthentication(!builder.Environment.IsDevelopment());

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWebApp", policy =>
                {
                    var webUrl = builder.Configuration.GetValue<string>("URL:Web:HTTPS")
                        ?? throw new InvalidOperationException("URL не прописан в appsettings.Development.json");

                    policy.WithOrigins(webUrl)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwaggerInterface();
            }

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/hubs/chat") ||
                    context.Request.Path.StartsWithSegments("/hubs/userstatus") ||
                    context.Request.Path.StartsWithSegments("/hubs/notification"))
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) &&
                        (context.Request.Headers.Authorization.Count == 0 ||
                         !context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Request.Headers.Authorization = $"Bearer {accessToken}";
                    }
                }

                await next();
            });

            app.UseUploads();
            app.UseCors("AllowWebApp");
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.MapHub<ChatHub>("/hubs/chat");
            app.MapHub<UserStatusHub>("/hubs/userstatus");
            app.MapHub<NotificationHub>("/hubs/notification");

            app.Run();
        }
    }
}

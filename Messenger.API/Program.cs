using Messenger.API.Extensions;
using Messenger.Core.Hubs;
using Microsoft.Extensions.FileProviders;

namespace Messenger.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (!builder.Environment.IsDevelopment())
            {
                JwtExtension.SetTheEnvironmentVariable(forMachine: false);
                PostgreSQLExtension.SetTheEnvironmentVariable(forMachine: false);
            }

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables();

            builder.Services.AddControllers();

            builder.Services.AddSignalRService();

            builder.Services.AddSwagger();
            builder.Services.AddPostgreSQL(builder.Configuration);
            builder.Services.AddRepositories();
            builder.Services.AddServices();
            builder.Services.AddJwtService(builder.Configuration);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWebApp", policy =>
                {
                    policy.WithOrigins("https://localhost:7010")
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

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsPath),
                RequestPath = "/uploads",
                OnPrepareResponse = ctx =>
                {
                    var path = ctx.Context.Request.Path.Value?.ToLowerInvariant() ?? "";
                    var query = ctx.Context.Request.QueryString.Value ?? "";

                    var imageExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg", ".avif" };

                    var extension = Path.GetExtension(path);

                    if (!imageExtensions.Contains(extension) || query.Contains("download"))
                    {
                        var fileName = Path.GetFileName(path);
                        ctx.Context.Response.Headers.ContentDisposition =
                            $"attachment; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";
                    }
                }
            });

            var avatarsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");

            if (!Directory.Exists(avatarsPath))
            {
                Directory.CreateDirectory(avatarsPath);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(avatarsPath),
                RequestPath = "/avatars"
            });

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

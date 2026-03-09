using Messenger.API.Extensions;
using Messenger.Core.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
                JwtExtension.SetTheEnvironmentVariable();
                PostgreSQLExtension.SetTheEnvironmentVariable();
            }

            builder.Services.AddControllers();

            builder.Services.AddSignalRService();

            builder.Services.AddSwagger();
            builder.Services.AddPostgreSQL(builder.Configuration);
            builder.Services.AddRepositories();
            builder.Services.AddServices();

            builder.Services.AddHttpClient();

            var useKeycloak = builder.Configuration.GetValue("Auth:UseKeycloak", false);

            if (builder.Environment.IsDevelopment() && !useKeycloak)
            {
                builder.Services.AddJwtService(builder.Configuration);
            }
            else
            {
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Authority = "https://sso.guap.ru/realms/master";
                        options.Audience = "messager";
                        options.TokenValidationParameters.ValidateIssuer = true;
                        options.TokenValidationParameters.ValidIssuer = "https://sso.guap.ru/realms/master";

                        options.TokenValidationParameters.ValidateAudience = true;
                        options.TokenValidationParameters.ValidAudiences = new[]
                        {
                            "messager",
                            "account",
                            "https://sso.guap.ru/realms/master"
                        };
                        options.TokenValidationParameters.ValidAudience = "messager";

                        options.TokenValidationParameters.ValidateLifetime = false;
                        options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(10);

                        options.Events = new JwtBearerEvents
                        {
                            OnAuthenticationFailed = ctx =>
                            {
                                Console.WriteLine($"JWT Auth failed: {ctx.Exception.Message}");
                                return Task.CompletedTask;
                            },
                            OnTokenValidated = ctx =>
                            {
                                Console.WriteLine("JWT Token validated");
                                return Task.CompletedTask;
                            }
                        };
                    });
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            });

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

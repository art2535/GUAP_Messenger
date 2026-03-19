using Messenger.API.Extensions;
using Messenger.Core.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

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

            builder.Services.AddHttpClient();

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = "https://sso.guap.ru/realms/master";
                    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "https://sso.guap.ru/realms/master",

                        ValidateAudience = true,
                        ValidAudiences = ["messager", "account"],

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(10),

                        NameClaimType = "sub",
                        RoleClaimType = "role",

                        ValidateIssuerSigningKey = true
                    };

                    options.MapInboundClaims = false;
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

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/hubs/chat") ||
                    context.Request.Path.StartsWithSegments("/hubs/userstatus") ||
                    context.Request.Path.StartsWithSegments("/hubs/notification"))
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) &&
                        (context.Request.Headers["Authorization"].Count == 0 ||
                         !context.Request.Headers["Authorization"].ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
                    }
                }

                await next();
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

using Messenger.API.Extensions;
using Messenger.Core.Hubs;
using Messenger.Web.Middleware;

namespace Messenger.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddPostgreSQL(builder.Configuration);
            builder.Services.AddRazorPages();
            builder.Services.AddEtaWebAuthentication(builder.Configuration);
            builder.Services.AddLogging();
            builder.Services.AddAuthorization();
            builder.Services.AddRabbitMQ(builder.Configuration);
            builder.Services.AddServices();
            builder.Services.AddRepositories();
            builder.Services.AddSignalRService();
            builder.Services.AddHttpClient();
            builder.Services.AddEncryption(builder.Configuration);
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.Map("/uploads/{**path}", (string path, HttpContext ctx) =>
            {
                var targetUrl = $"{builder.Configuration["URL:API:HTTPS"]}/uploads/{path}{ctx.Request.QueryString}";
                return Results.Redirect(targetUrl, false);
            });

            app.UseSession();
            app.UseRouting();

            app.UseAuthentication();
            app.UseMiddleware<TokenRefreshMiddleware>();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapStaticAssets();
            app.MapHub<ChatHub>("/hubs/chat");
            app.MapControllers();

            app.Run();
        }
    }
}

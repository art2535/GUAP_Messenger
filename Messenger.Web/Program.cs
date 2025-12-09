using Messenger.API.Extensions;
using Messenger.Core.Hubs;

namespace Messenger.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddPostgreSQL(builder.Configuration);
            builder.Services.AddJwtService(builder.Configuration);
            builder.Services.AddRazorPages();
            builder.Services.AddServices();
            builder.Services.AddRepositories();
            builder.Services.AddSignalRService();
            builder.Services.AddHttpClient();

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
                var targetUrl = $"https://localhost:7001/uploads/{path}{ctx.Request.QueryString}";
                return Results.Redirect(targetUrl, permanent: false);
            });

            app.UseSession();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapStaticAssets();
            app.MapHub<ChatHub>("/hubs/chat");
            app.MapControllers();

            app.Run();
        }
    }
}

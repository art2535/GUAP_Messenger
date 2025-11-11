using Messenger.API.Extensions;

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
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapStaticAssets();

            app.Run();
        }
    }
}

using Microsoft.Extensions.FileProviders;

namespace Messenger.API.Extensions
{
    public static class StaticFilesExtension
    {
        public static void UseUploads(this WebApplication app)
        {
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
        }
    }
}

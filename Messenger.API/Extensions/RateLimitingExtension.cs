using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Messenger.API.Extensions
{
    public static class RateLimitingExtension
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddMessengerRateLimiting()
            {
                services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                    options.AddFixedWindowLimiter("api", opt =>
                    {
                        opt.PermitLimit = 120;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 20;
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    });

                    options.AddFixedWindowLimiter("send-message", opt =>
                    {
                        opt.PermitLimit = 30;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 5;
                    });

                    options.AddFixedWindowLimiter("typing", opt =>
                    {
                        opt.PermitLimit = 60;
                        opt.Window = TimeSpan.FromMinutes(1);
                    });

                    options.OnRejected = async (context, ct) =>
                    {
                        context.HttpContext.Response.ContentType = "application/json";
                        await context.HttpContext.Response.WriteAsync(
                            """{"isSuccess":false,"error":"Too many requests. Please slow down."}""", ct);
                    };
                });

                return services;
            }
        }
    }
}

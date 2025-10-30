using Messenger.API.Extensions;
using Messenger.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Reflection;

namespace Messenger.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("The Messenger service is started at: {time}", DateTimeOffset.Now);

            try
            {
                var solutionPath = @"D:\IDE Projects\Visual Studio\Диплом\GUAP_Messenger";
                var webProjectPath = Path.Combine(solutionPath, "Messenger.Web");
                var webRootPath = Path.Combine(webProjectPath, "wwwroot");

                _logger.LogInformation("WebRootPath set to: {path}", webRootPath);

                var builder = WebApplication.CreateBuilder(new WebApplicationOptions
                {
                    ContentRootPath = webProjectPath,
                    WebRootPath = webRootPath
                });

                builder.Configuration
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                    .AddUserSecrets<Worker>(optional: true)
                    .AddEnvironmentVariables();

                var webApiUrl = builder.Configuration.GetValue<string>("Kestrel:Endpoints:WebApi:Url")
                                ?? "https://localhost:7045";
                var razorPagesUrl = builder.Configuration.GetValue<string>("Kestrel:Endpoints:RazorPages:Url")
                                    ?? "https://localhost:7128";

                var webApiPort = new Uri(webApiUrl).Port;
                var razorPagesPort = new Uri(razorPagesUrl).Port;

                builder.WebHost.ConfigureKestrel(options =>
                {
                    _logger.LogInformation("Configuring Kestrel endpoints from configuration");

                    options.ListenLocalhost(webApiPort, listenOptions => listenOptions.UseHttps());
                    options.ListenLocalhost(razorPagesPort, listenOptions => listenOptions.UseHttps());
                });

                if (!builder.Environment.IsDevelopment())
                {
                    JwtExtension.SetTheEnvironmentVariable(forMachine: false);
                    PostgreSQLExtension.SetTheEnvironmentVariable(forMachine: false);
                }

                builder.Services.AddControllers().AddApplicationPart(Assembly.Load("Messenger.API"));
                builder.Services.AddRazorPages()
                    .AddApplicationPart(Assembly.Load("Messenger.Web"))
                    .AddRazorPagesOptions(options => options.RootDirectory = "/Pages");

                builder.Services.AddHttpClient();
                builder.Services.AddSwagger();
                builder.Services.AddPostgreSQL(builder.Configuration);
                builder.Services.AddRepositories();
                builder.Services.AddServices();
                builder.Services.AddJwtService(builder.Configuration);
                builder.Services.AddWebSockets(options => { });

                builder.Services.AddDistributedMemoryCache();
                builder.Services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(30);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowWebApp", policy =>
                    {
                        _logger.LogInformation("CORS configured for origins: {url}, https://localhost:7045", razorPagesUrl);
                        policy.WithOrigins(razorPagesUrl, "https://localhost:7045")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials()
                              .SetIsOriginAllowedToAllowWildcardSubdomains();
                    });
                });

                var app = builder.Build();

                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        var logger = context.RequestServices.GetRequiredService<ILogger<Worker>>();
                        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                        logger.LogError(exception, "Unhandled exception occurred.");
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Internal Server Error");
                    });
                });

                app.UseCors("AllowWebApp");
                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseSession();
                app.UseAuthentication();
                app.UseAuthorization();

                app.MapWhen(context => context.Request.Host.Port == webApiPort, webApi =>
                {
                    _logger.LogInformation("Configuring Web API and Swagger on port {port}", webApiPort);
                    if (app.Environment.IsDevelopment())
                    {
                        webApi.UseSwaggerInterface();
                    }
                    webApi.UseRouting();
                    webApi.UseAuthentication();
                    webApi.UseAuthorization();
                    webApi.UseEndpoints(endpoints => endpoints.MapControllers());
                });

                app.MapWhen(context => context.Request.Host.Port == razorPagesPort, razorPages =>
                {
                    _logger.LogInformation("Configuring Razor Pages on port {port}", razorPagesPort);

                    razorPages.Use(async (context, next) =>
                    {
                        if (context.Request.Path.Value?.EndsWith("index.html", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            context.Response.Redirect("/", permanent: true);
                            return;
                        }
                        await next();
                    });

                    razorPages.UseStaticFiles(new StaticFileOptions
                    {
                        OnPrepareResponse = ctx =>
                            _logger.LogInformation("Serving static file: {path}", ctx.File.PhysicalPath)
                    });

                    razorPages.UseRouting();
                    razorPages.UseAuthentication();
                    razorPages.UseAuthorization();
                    razorPages.UseWebSockets();
                    razorPages.UseSession();

                    razorPages.UseEndpoints(endpoints =>
                    {
                        endpoints.MapRazorPages();
                        endpoints.Map("/ws", async context =>
                        {
                            if (context.WebSockets.IsWebSocketRequest)
                            {
                                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                                _logger.LogInformation("WebSocket connection established on port {port}", razorPagesPort);
                                await context.Response.WriteAsync("WebSocket connected");
                            }
                            else
                            {
                                context.Response.StatusCode = 400;
                            }
                        });
                    });
                });

                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<GuapMessengerContext>();
                    await db.Database.MigrateAsync(stoppingToken);
                    _logger.LogInformation("Database migrations have been successfully applied.");
                }

                _logger.LogInformation("Starting Web API on {webApiUrl} and Razor Pages on {razorPagesUrl}", webApiUrl, razorPagesUrl);
                await app.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("A service stop has been requested.");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                _logger.LogError(ex, "Failed to bind to port. Ensure ports are not in use by other processes.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker Background Service error.");
                throw;
            }
            finally
            {
                _logger.LogInformation("The Messenger service is stopped at: {time}", DateTimeOffset.Now);
            }
        }
    }
}

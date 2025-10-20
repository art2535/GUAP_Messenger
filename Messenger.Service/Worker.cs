using Messenger.API.Extensions;
using Messenger.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
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
            _logger.LogInformation("The Messenger service is started in: {time}", DateTimeOffset.Now);

            try
            {
                var builder = WebApplication.CreateBuilder();

                if (!builder.Environment.IsDevelopment())
                {
                    JwtExtension.SetTheEnvironmentVariable(forMachine: false);
                    PostgreSQLExtension.SetTheEnvironmentVariable(forMachine: false);
                }

                builder.Configuration
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                    .AddUserSecrets<Worker>(optional: true)
                    .AddEnvironmentVariables();

                builder.Services.AddControllers()
                    .AddApplicationPart(Assembly.Load("Messenger.API"));

                builder.Services.AddSwagger();

                builder.Services.AddPostgreSQL(builder.Configuration);
                builder.Services.AddRepositories();
                builder.Services.AddServices();
                builder.Services.AddJwtService(builder.Configuration);

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowWebApp", policy =>
                    {
                        policy.WithOrigins("https://localhost:7128")
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
                });

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwaggerInterface();
                }

                app.UseCors("AllowWebApp");
                app.UseHttpsRedirection();
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapControllers();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<GuapMessengerContext>();
                    await db.Database.MigrateAsync(stoppingToken);

                    _logger.LogInformation("Database migrations have been successfully applied.");
                }

                _logger.LogInformation("The built-in Web API is running.");

                await app.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("A service stop has been requested.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker Background Service error.");
            }
            finally
            {
                _logger.LogInformation("The Messenger service is stopped in: {time}", DateTimeOffset.Now);
            }
        }
    }
}

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
            _logger.LogInformation("Служба Messenger запускается в: {time}", DateTimeOffset.Now);

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

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwaggerInterface();
                }

                app.UseHttpsRedirection();
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapControllers();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<GuapMessengerContext>();
                    await db.Database.MigrateAsync(stoppingToken);

                    _logger.LogInformation("Миграции базы данных успешно применены.");
                }

                _logger.LogInformation("Встроенный Web API запущен.");

                await app.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Запрошена остановка службы.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка фонового сервиса Worker");
            }
            finally
            {
                _logger.LogInformation("Служба Messenger остановлена в: {time}", DateTimeOffset.Now);
            }
        }
    }
}

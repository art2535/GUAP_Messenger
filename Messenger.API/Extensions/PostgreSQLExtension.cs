using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.API.Extensions
{
    public static class PostgreSQLExtension
    {
        public static void AddPostgreSQL(this IServiceCollection services, IConfiguration? configuration = null)
        {
            var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString",
                EnvironmentVariableTarget.Machine) ?? string.Empty;

            if (string.IsNullOrEmpty(connectionString))
            {
                if (configuration is null)
                {
                    throw new InvalidOperationException(
                        "Не указана строка подключения к PostgreSQL. " +
                        "Добавьте переменную окружения 'PostgresConnectionString' " +
                        "или настройте 'DefaultConnection' в appsettings.json."
                    );
                }

                connectionString = configuration.GetConnectionString("DefaultConnection");
            }

            services.AddDbContext<GuapMessengerContext>(options =>
                options.UseNpgsql(connectionString));
        }

        public static void SetTheEnvironmentVariable(bool forMachine = false)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Development.json", true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Не удалось найти строку подключения 'DefaultConnection' в appsettings.Development.json!"
                );
            }

            var target = forMachine ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;

            Environment.SetEnvironmentVariable("PostgresConnectionString", connectionString, target);
        }
    }
}

using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

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

        public static void SetTheEnvironmentVariable(bool forMachine = true)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Не удалось найти строку подключения 'DefaultConnection' в secrets.json или appsettings.json."
                );
            }

            var target = forMachine ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;

            Environment.SetEnvironmentVariable("PostgresConnectionString", connectionString, target);
        }
    }
}

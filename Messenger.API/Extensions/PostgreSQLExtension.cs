using Messenger.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Messenger.API.Extensions
{
    /// <summary>
    /// Класс-расширение для подключения к СУБД PostgreSQL
    /// </summary>
    public static class PostgreSQLExtension
    {
        /// <summary>
        /// Регистрирует подключение к СУБД PostgreSQL
        /// </summary>
        public static void AddPostgreSQL(this IServiceCollection services, IConfiguration? configuration = null)
        {
            var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString",
                EnvironmentVariableTarget.Machine) ?? string.Empty;

            if (string.IsNullOrEmpty(connectionString))
            {
                if (configuration != null)
                {
                    services.AddDbContext<GuapMessengerContext>(options =>
                        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
                }
            }
            else
            {
                services.AddDbContext<GuapMessengerContext>(options =>
                    options.UseNpgsql(connectionString));
            }

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<GuapMessengerContext>()
                .AddDefaultTokenProviders();
        }
    }
}

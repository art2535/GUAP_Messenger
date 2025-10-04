using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace Messenger.API.Extensions
{
    /// <summary>
    /// Класс-расширение для JWT-аутентификации
    /// </summary>
    public static class JwtExtension
    {
        /// <summary>
        /// Регистрирует JWT-аутентификацию с использованием переменной окружения или конфигурационного файла.
        /// </summary>
        public static void AddJwtService(this IServiceCollection services, IConfiguration? configuration = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY", EnvironmentVariableTarget.User);

            if (string.IsNullOrEmpty(jwtSecretKey))
            {
                jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY", EnvironmentVariableTarget.Machine);
            }

            var rawKey = !string.IsNullOrEmpty(jwtSecretKey)
                ? Convert.FromBase64String(jwtSecretKey)
                : Encoding.UTF8.GetBytes(configuration["Jwt:Key"]);

            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(rawKey)
                    };
                });
        }

        /// <summary>
        /// Генерирует новый JWT-секрет и сохраняет его в переменную окружения (на уровне пользователя/машины).
        /// </summary>
        /// <param name="forMachine">Установка переменной на уровне машины</param>
        public static void SetTheEnvironmentVariable(bool forMachine = true)
        {
            var jwtSecretKey = GenerateJwtSecretKey();

            var target = forMachine ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;

            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", jwtSecretKey, target);
        }

        /// <summary>
        /// Генерирует криптографически стойкий ключ длиной 32 байта (по умолчанию).
        /// </summary>
        /// <param name="length">Длина ключа в байтах</param>
        /// <returns></returns>
        private static string GenerateJwtSecretKey(int length = 32)
        {
            byte[] bytes = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes);
        }
    }
}

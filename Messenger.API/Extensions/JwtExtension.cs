using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Security.Cryptography;

namespace Messenger.API.Extensions
{
    public static class JwtExtension
    {
        public static void AddJwtService(this IServiceCollection services, IConfiguration? configuration = null)
        {
            string? envKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY", EnvironmentVariableTarget.User)
                ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY", EnvironmentVariableTarget.Machine);

            byte[] rawKey;
            string selectedKey;

            if (!string.IsNullOrEmpty(envKey))
            {
                try
                {
                    rawKey = Convert.FromBase64String(envKey);
                    selectedKey = envKey;
                    Console.WriteLine($"AddJwtService: JWT ключ успешно получен из переменной окружения: " +
                        $"{envKey} (длина: {rawKey.Length} байт)");
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException("JWT_SECRET_KEY из переменной окружения " +
                        "не является корректной Base64 строкой.");
                }
            }
            else if (configuration != null && !string.IsNullOrEmpty(configuration["Jwt:Key"]))
            {
                try
                {
                    rawKey = Convert.FromBase64String(configuration["Jwt:Key"]);
                    selectedKey = configuration["Jwt:Key"];
                    Console.WriteLine($"AddJwtService: JWT ключ успешно получен из конфигурации: " +
                        $"{selectedKey} (длина: {rawKey.Length} байт)");
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException("Jwt:Key из конфигурации не является корректной Base64 строкой.");
                }
            }
            else
            {
                throw new InvalidOperationException("JWT ключ не найден. Настройте переменную окружения " +
                    "JWT_SECRET_KEY или параметр Jwt:Key в secrets.json / appsettings.json.");
            }

            if (rawKey.Length < 32)
            {
                throw new InvalidOperationException("JWT ключ слишком короткий. Минимальная длина для HMAC-SHA256 — 32 байта.");
            }

            var issuer = configuration?["Jwt:Issuer"];
            var audience = configuration?["Jwt:Audience"];

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

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Cookies["JWT_SECRET"];
                            if (!string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
        }

        public static void SetTheEnvironmentVariable(bool forMachine = true)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
                .AddEnvironmentVariables()
                .Build();

            var jwtSecretKey = configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtSecretKey))
            {
                jwtSecretKey = GenerateJwtSecretKey();
                Console.WriteLine("JWT ключ не найден в конфигурации. Сгенерирован временный ключ.");
            }

            var target = forMachine ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", jwtSecretKey, target);
            Console.WriteLine($"JWT_SECRET_KEY установлен в переменную окружения ({target}).");
        }

        private static string GenerateJwtSecretKey(int length = 64)
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

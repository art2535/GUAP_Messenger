using Messenger.Core.Interfaces;
using Messenger.Infrastructure.Services;
using System.Security.Cryptography;
using System.Text.Json;

namespace Messenger.API.Extensions
{
    public static class DevelopmentEncryptionKeyExtension
    {
        public static WebApplicationBuilder EnsureSharedDevelopmentEncryptionKey(this WebApplicationBuilder builder)
        {
            if (!builder.Environment.IsDevelopment())
                return builder;

            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (currentDir != null && !currentDir.GetFiles("*.sln").Any())
            {
                currentDir = currentDir.Parent;
            }

            if (currentDir == null)
            {
                Console.WriteLine("Корень решения не найден, использую локальный ключ.");
                return builder;
            }

            var sharedKeyPath = Path.Combine(currentDir.FullName, "shared-dev.key");
            string masterKey;

            if (File.Exists(sharedKeyPath))
            {
                masterKey = File.ReadAllText(sharedKeyPath);
            }
            else
            {
                masterKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                File.WriteAllText(sharedKeyPath, masterKey);
                Console.WriteLine(">>> Сгенерирован НОВЫЙ общий ключ для всей разработки.");
            }

            UpdateProjectSettings(masterKey);

            builder.Configuration["Encryption:MasterKeyBase64"] = masterKey;

            return builder;
        }

        private static void UpdateProjectSettings(string key)
        {
            var devSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json");
            if (!File.Exists(devSettingsPath))
                return;

            try
            {
                var json = File.ReadAllText(devSettingsPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();

                var currentKeyInJson = "";
                if (config.TryGetValue("Encryption", out var encObj) && encObj is JsonElement encElem)
                {
                    if (encElem.TryGetProperty("MasterKeyBase64", out var keyProp))
                        currentKeyInJson = keyProp.GetString();
                }

                if (currentKeyInJson != key)
                {
                    config["Encryption"] = new { MasterKeyBase64 = key };
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(devSettingsPath, JsonSerializer.Serialize(config, options));
                    Console.WriteLine($"[Encryption] Ключ ОБНОВЛЕН в {Path.GetFileName(devSettingsPath)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка синхронизации ключа: {ex.Message}");
            }
        }

        public static void AddEncryption(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AesGcmEncryptionOptions>(configuration.GetSection("Encryption"));
            services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();
        }
    }
}

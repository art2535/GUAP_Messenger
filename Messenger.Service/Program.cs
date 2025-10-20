using Messenger.API.Extensions;

namespace Messenger.Service
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                    config.AddUserSecrets<Worker>(optional: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddPostgreSQL(context.Configuration);

                    services.AddRepositories();
                    services.AddServices();
                    services.AddJwtService(context.Configuration);

                    services.AddHostedService<Worker>();
                })
                .Build();

            await builder.RunAsync();
        }
    }
}
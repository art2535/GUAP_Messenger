using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Messenger.API.Extensions
{
    public static class OpenApiExtension
    {
        extension(IServiceCollection services)
        {
            public void AddScalarApi()
            {
                services.AddApiVersioning()
                    .AddApiExplorer(options =>
                    {
                        options.GroupNameFormat = "'v'VVV";
                        options.SubstituteApiVersionInUrl = true;
                    });

                services.AddOpenApi("v1", options =>
                {
                    options.AddDocumentTransformer((document, context, cancellationToken) =>
                    {
                        document.Info = new OpenApiInfo
                        {
                            Title = "Messenger API",
                            Version = "v1",
                            Description = "API для управления мессенджером ГУАП"
                        };
                        return Task.CompletedTask;
                    });
                });
            }
        }

        extension(WebApplication app)
        {
            public void MapScalarApi()
            {
                app.MapOpenApi("/openapi/{documentName}.json");

                app.MapScalarApiReference(options =>
                {
                    options.WithTitle("Messenger API")
                        .WithClassicLayout()
                        .WithTheme(ScalarTheme.Purple)
                        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                        .AddDocument("v1", "Messenger API v1", "/openapi/v1.json");
                });
            }
        }
    }
}

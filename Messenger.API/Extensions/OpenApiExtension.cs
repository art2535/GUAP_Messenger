using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Messenger.API.Extensions
{
    public static class OpenApiExtension
    {
        extension(IServiceCollection services)
        {
            public void AddScalarApi(IConfiguration configuration)
            {
                services.AddApiVersioning()
                    .AddApiExplorer(options =>
                    {
                        options.GroupNameFormat = "'v'VVV";
                        options.SubstituteApiVersionInUrl = true;
                    });

                var authority = $"{configuration["AzureAd:Instance"]?.TrimEnd('/')}/{configuration["AzureAd:TenantId"]}";

                Action<OpenApiDocument, OpenApiDocumentTransformerContext, CancellationToken> commonSecurityTransformer =
                    (document, context, cancellationToken) =>
                    {
                        document.Components ??= new OpenApiComponents();
                        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                        document.Components.SecuritySchemes["OAuth2"] = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.OAuth2,
                            Description = "OIDC SSO ГУАП",
                            Flows = new OpenApiOAuthFlows
                            {
                                AuthorizationCode = new OpenApiOAuthFlow
                                {
                                    AuthorizationUrl = new Uri($"{authority}/protocol/openid-connect/auth"),
                                    TokenUrl = new Uri($"{configuration["URL:API:HTTPS"]}/oauth/token"),
                                    Scopes = new Dictionary<string, string>
                                    {
                                        ["openid"] = "OpenID Connect",
                                        ["profile"] = "Профиль",
                                        ["email"] = "Email",
                                        ["roles"] = "Роли",
                                        ["offline_access"] = "Refresh token"
                                    }
                                }
                            }
                        };

                        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            BearerFormat = "JWT",
                            Description = "Вставьте access_token вручную"
                        };
                    };

                void AddOpenApiVersion(string version, string title, string description)
                {
                    services.AddOpenApi(version, options =>
                    {
                        options.AddDocumentTransformer((document, context, cancellationToken) =>
                        {
                            document.Info = new OpenApiInfo
                            {
                                Title = title,
                                Version = version,
                                Description = description
                            };

                            commonSecurityTransformer(document, context, cancellationToken);

                            return Task.CompletedTask;
                        });
                    });
                }

                AddOpenApiVersion("v1", "Messenger API", "API для управления мессенджером ГУАП");
            }
        }

        extension(WebApplication app)
        {
            public void MapScalarApi(IConfiguration configuration)
            {
                app.MapOpenApi("/openapi/{documentName}.json");

                app.MapScalarApiReference(options =>
                {
                    options.WithTitle("Messenger API")
                        .WithClassicLayout()
                        .WithTheme(ScalarTheme.Purple)
                        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                        .AddDocument("v1", "Messenger API v1", "/openapi/v1.json")
                        .AddPreferredSecuritySchemes("OAuth2")
                        .AddAuthorizationCodeFlow("OAuth2", flow =>
                        {
                            flow.ClientId = configuration["AzureAd:ClientId"];
                            flow.ClientSecret = configuration["AzureAd:ClientSecret"];
                            flow.Pkce = Pkce.Sha256;
                            flow.SelectedScopes = ["openid", "profile", "email", "roles", "offline_access"];
                            flow.WithCredentialsLocation(CredentialsLocation.Body);
                            flow.RedirectUri = $"{configuration["URL:API:HTTPS"]}/scalar/*";
                        })
                        .AddHttpAuthentication("Bearer", _ => { });
                });
            }
        }
    }
}

using Messenger.API.Extensions;
using Messenger.Core.Hubs;
using Messenger.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Messenger.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddPostgreSQL(builder.Configuration);

            builder.Services.AddRazorPages();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Authorization";
                options.ExpireTimeSpan = TimeSpan.FromHours(12);
                options.SlidingExpiration = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = "https://sso.guap.ru/realms/master";
                options.ClientId = builder.Configuration["AzureAd:ClientId"];
                options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
                options.CallbackPath = builder.Configuration["AzureAd:CallbackPath"];
                options.SignedOutCallbackPath = builder.Configuration["AzureAd:SignedOutCallbackPath"];
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";
                options.CallbackPath = "/signin-oidc";

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = ctx =>
                    {
                        Console.WriteLine("Редирект на Keycloak");
                        return Task.CompletedTask;
                    },

                    OnTokenValidated = ctx =>
                    {
                        Console.WriteLine("Token валидирован!");
                        return Task.CompletedTask;
                    },

                    OnTicketReceived = ctx =>
                    {
                        Console.WriteLine("OnTicketReceived СРАБОТАЛ!");
                        return Task.CompletedTask;
                    },

                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine($"OIDC Failed: {ctx.Exception.Message}");
                        ctx.Response.Redirect("/Authorization/Authorization?error=auth_failed");
                        ctx.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddServices();
            builder.Services.AddRepositories();
            builder.Services.AddSignalRService();
            builder.Services.AddHttpClient();
            builder.Services.AddEncryption(builder.Configuration);

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.Map("/uploads/{**path}", (string path, HttpContext ctx) =>
            {
                var targetUrl = $"{builder.Configuration["URL:API:HTTPS"]}/uploads/{path}{ctx.Request.QueryString}";
                return Results.Redirect(targetUrl, false);
            });

            app.UseSession();
            app.UseRouting();

            app.UseAuthentication();
            app.UseMiddleware<TokenRefreshMiddleware>();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapStaticAssets();
            app.MapHub<ChatHub>("/hubs/chat");
            app.MapControllers();

            app.Run();
        }
    }
}

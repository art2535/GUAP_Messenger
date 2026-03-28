using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Messenger.API.Extensions
{
    public static class AuthenticationExtension
    {
        public static void AddEtaApiAuthentication(this IServiceCollection services, bool requireHttpsMetadata)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = "https://sso.guap.ru/realms/master";
                    options.RequireHttpsMetadata = requireHttpsMetadata;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "https://sso.guap.ru/realms/master",

                        ValidateAudience = true,
                        ValidAudiences = ["messager", "account"],

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(10),

                        NameClaimType = "sub",
                        RoleClaimType = "role",

                        ValidateIssuerSigningKey = true
                    };

                    options.MapInboundClaims = false;
                });
        }

        public static void AddEtaWebAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
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
                options.ClientId = configuration["AzureAd:ClientId"];
                options.ClientSecret = configuration["AzureAd:ClientSecret"];
                options.CallbackPath = configuration["AzureAd:CallbackPath"];
                options.SignedOutCallbackPath = configuration["AzureAd:SignedOutCallbackPath"];
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
            });
        }
    }
}

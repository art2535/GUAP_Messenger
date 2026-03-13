using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;

namespace Messenger.Web.Middleware
{
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public TokenRefreshMiddleware(RequestDelegate next, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _next = next;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                string? accessToken = await context.GetTokenAsync("access_token");
                string? refreshToken = await context.GetTokenAsync("refresh_token");

                if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
                {
                    Console.WriteLine($"[TokenRefresh] Текущий токен (первые 20): {accessToken?.Substring(0, 20) ?? "null"}");

                    try
                    {
                        var jwtHandler = new JwtSecurityTokenHandler();
                        var jwt = jwtHandler.ReadJwtToken(accessToken);
                        var timeLeft = jwt.ValidTo - DateTime.UtcNow;

                        if (timeLeft < TimeSpan.FromMinutes(1))
                        {
                            Console.WriteLine($"[TokenRefresh] Токен истекает через {timeLeft.TotalSeconds} сек — обновляем");

                            var client = _httpClientFactory.CreateClient();
                            var tokenEndpoint = "https://sso.guap.ru/realms/master/protocol/openid-connect/token";

                            var formData = new Dictionary<string, string>
                            {
                                { "grant_type", "refresh_token" },
                                { "refresh_token", refreshToken },
                                { "client_id", _configuration["AzureAd:ClientId"] ?? "messager" },
                                { "client_secret", _configuration["AzureAd:ClientSecret"] ?? "" }
                            };

                            var content = new FormUrlEncodedContent(formData);
                            var response = await client.PostAsync(tokenEndpoint, content);

                            if (response.IsSuccessStatusCode)
                            {
                                var json = await response.Content.ReadAsStringAsync();
                                var tokenResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                                var newAccessToken = tokenResponse?["access_token"]?.ToString();
                                var newRefreshToken = tokenResponse?["refresh_token"]?.ToString() ?? refreshToken;

                                if (!string.IsNullOrEmpty(newAccessToken))
                                {
                                    var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                    if (authResult.Succeeded)
                                    {
                                        authResult.Properties.UpdateTokenValue("access_token", newAccessToken);
                                        authResult.Properties.UpdateTokenValue("refresh_token", newRefreshToken);

                                        await context.SignInAsync(
                                            CookieAuthenticationDefaults.AuthenticationScheme,
                                            authResult.Principal!,
                                            authResult.Properties);

                                        Console.WriteLine("[TokenRefresh] Токен успешно обновлён");
                                    }
                                }
                            }
                            else
                            {
                                var error = await response.Content.ReadAsStringAsync();
                                Console.WriteLine($"[TokenRefresh] Ошибка обновления: {response.StatusCode} {error}");

                                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                                    response.StatusCode == HttpStatusCode.Forbidden)
                                {
                                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                    Console.WriteLine("[TokenRefresh] Refresh_token истёк — принудительный выход");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TokenRefresh] Ошибка: {ex.Message}");
                    }
                }
            }

            await _next(context);
        }
    }
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Messenger.Web.Pages.Account
{
    [Authorize]
    public class ChatsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public string? UserId { get; set; }
        public string? UserName { get; set; } = string.Empty;
        public string? UserRole { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public ChatsModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            bool isOidcAuthenticated = User.Identity?.IsAuthenticated == true;

            if (isOidcAuthenticated)
            {
                UserName = User.FindFirstValue("name")
                        ?? User.FindFirstValue("preferred_username")
                        ?? "Пользователь ЕТА";

                UserId = User.FindFirstValue("sub");

                UserRole = User.FindFirstValue("role")
                        ?? User.FindFirstValue("roles")
                        ?? "Пользователь";

                var accessToken = await HttpContext.GetTokenAsync("access_token");
                var idToken = await HttpContext.GetTokenAsync("id_token");
                var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

                HttpContext.Session.SetString("ACCESS_TOKEN", accessToken ?? "");
                HttpContext.Session.SetString("ID_TOKEN", idToken ?? "");
                HttpContext.Session.SetString("REFRESH_TOKEN", refreshToken ?? "");

                HttpContext.Session.SetString("USER_ID", UserId ?? "");
                HttpContext.Session.SetString("USER_NAME", UserName);
                HttpContext.Session.SetString("USER_ROLE", UserRole);
            }
            else
            {
                var token = HttpContext.Session.GetString("JWT_TOKEN");
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToPage("/Authorization/Authorization");
                }

                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    HttpContext.Session.Clear();
                    return RedirectToPage("/Authorization/Authorization");
                }

                var jwtToken = handler.ReadJwtToken(token);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    HttpContext.Session.Clear();
                    return RedirectToPage("/Authorization/Authorization");
                }

                UserId = HttpContext.Session.GetString("USER_ID");
                UserName = HttpContext.Session.GetString("USER_NAME") ?? "Локальный пользователь";
                UserRole = HttpContext.Session.GetString("USER_ROLE") ?? "Пользователь";
            }

            string? authToken = null;
            if (isOidcAuthenticated)
            {
                authToken = await HttpContext.GetTokenAsync("access_token");
            }
            authToken ??= HttpContext.Session.GetString("JWT_TOKEN");

            if (!string.IsNullOrEmpty(authToken))
            {
                try
                {
                    Console.WriteLine($"Access Token - {authToken}");
                    var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                    var response = await client.GetAsync($"{_configuration["URL:API:HTTPS"]}/api/users/info");

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonText = await response.Content.ReadAsStringAsync();
                        var jsonDoc = JsonDocument.Parse(jsonText);

                        if (jsonDoc.RootElement.TryGetProperty("data", out var data) &&
                            data.TryGetProperty("account", out var account) &&
                            account.TryGetProperty("avatar", out var avatar) &&
                            avatar.ValueKind == JsonValueKind.String)
                        {
                            AvatarUrl = avatar.GetString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка получения аватара: {ex.Message}");
                }
            }

            return Page();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Messenger.Web.Pages.Account
{
    public class ChatsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public string? UserId { get; set; }
        public string? JwtToken { get; set; }
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
            var token = HttpContext.Session.GetString("JWT_TOKEN");

            if (string.IsNullOrEmpty(token))
                return RedirectToPage("/Authorization/Authorization");

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Authorization/Authorization");
            }

            UserId = HttpContext.Session.GetString("USER_ID");
            UserName = HttpContext.Session.GetString("USER_NAME");
            UserRole = HttpContext.Session.GetString("USER_ROLE");

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync($"{_configuration["URL:API:HTTPS"]}/api/users/info");

                if (response.IsSuccessStatusCode)
                {
                    var jsonText = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(jsonText);

                    if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement) &&
                        dataElement.TryGetProperty("account", out var accountElement) &&
                        accountElement.TryGetProperty("avatar", out var avatarElement) &&
                        avatarElement.ValueKind == JsonValueKind.String)
                    {
                        var avatarPath = avatarElement.GetString();
                        if (!string.IsNullOrWhiteSpace(avatarPath))
                        {
                            AvatarUrl = avatarPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения данных: {ex.Message}");
            }

            return Page();
        }
    }
}

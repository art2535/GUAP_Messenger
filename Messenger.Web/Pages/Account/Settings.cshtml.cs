using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;
using Messenger.Core.DTOs.Users;

namespace Messenger.Web.Pages.Account
{
    public class SettingsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SettingsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public UpdateUserProfileRequest Profile { get; set; } = new();

        [BindProperty]
        public string? AvatarUrl { get; set; }


        public JsonElement CurrentUserJson { get; private set; }
        public IReadOnlyList<JsonElement> BlockedUsers { get; private set; } = Array.Empty<JsonElement>();

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("JWT_SECRET");

            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Authorization/Authorization");
            }

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var client = CreateClient();

            var payload = new
            {
                Profile.LastName,
                Profile.FirstName,
                Profile.MiddleName,
                Profile.Login,
                Profile.Phone,
                Profile.Theme
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync("https://localhost:7001/api/users/update-profile", content);

            if (!string.IsNullOrEmpty(AvatarUrl))
            {
                await client.PutAsync($"https://localhost:7001/api/users/update-profile?avatarUrl={Uri.EscapeDataString(AvatarUrl)}", null!);
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Профиль успешно обновлён";
                return RedirectToPage();
            }

            ModelState.AddModelError("", "Ошибка сохранения профиля");
            await LoadDataAsync();
            return Page();
        }

        private async Task LoadDataAsync()
        {
            var client = CreateClient();
            try
            {
                var userResp = await client.GetAsync("https://localhost:7001/api/users/info");
                if (userResp.IsSuccessStatusCode)
                {
                    var json = await userResp.Content.ReadAsStringAsync();
                    var root = JsonSerializer.Deserialize<JsonElement>(json);
                    if (root.TryGetProperty("data", out var data))
                    {
                        CurrentUserJson = data;
                        Profile = new UpdateUserProfileRequest
                        {
                            LastName = data.TryGetProperty("lastName", out var ln) ? ln.GetString() ?? "" : "",
                            FirstName = data.TryGetProperty("firstName", out var fn) ? fn.GetString() ?? "" : "",
                            MiddleName = data.TryGetProperty("middleName", out var mn) ? mn.GetString() : null,
                            Login = data.TryGetProperty("login", out var log) ? log.GetString() ?? "" : "",
                            Phone = data.TryGetProperty("phone", out var ph) ? ph.GetString() ?? "" : "",
                            Theme = data.TryGetProperty("theme", out var th) ? th.GetString() : "light"
                        };
                    }
                }
                else
                {
                    CurrentUserJson = JsonDocument.Parse("{}").RootElement;
                }

                var blockedResp = await client.GetAsync("https://localhost:7001/api/users/blocked");
                if (blockedResp.IsSuccessStatusCode)
                {
                    var json = await blockedResp.Content.ReadAsStringAsync();
                    var root = JsonSerializer.Deserialize<JsonElement>(json);
                    if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        BlockedUsers = dataArray.EnumerateArray().ToList();
                    }
                    else
                    {
                        BlockedUsers = Array.Empty<JsonElement>();
                    }
                }
                else
                {
                    BlockedUsers = Array.Empty<JsonElement>();
                }
            }
            catch (Exception ex)
            {
                CurrentUserJson = JsonDocument.Parse("{}").RootElement;
                BlockedUsers = Array.Empty<JsonElement>();
            }
        }

        // Добавь этот обработчик в SettingsModel
        public async Task<IActionResult> OnGetSearchUsersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new JsonResult(new List<object>());

            var client = CreateClient();
            var response = await client.GetAsync($"https://localhost:7001/api/users/search?query={Uri.EscapeDataString(query)}");

            if (!response.IsSuccessStatusCode)
                return new JsonResult(new List<object>());

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonSerializer.Deserialize<JsonElement>(json);

            var users = new List<object>();

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var user in data.EnumerateArray())
                {
                    var id = user.TryGetProperty("id", out var i) ? i.GetString() : null;
                    var name = user.TryGetProperty("name", out var n) ? n.GetString() : "Без имени";
                    var avatar = user.TryGetProperty("avatar", out var a) ? a.GetString() : null;

                    if (string.IsNullOrEmpty(id)) continue;

                    // Исключаем уже заблокированных
                    bool isBlocked = BlockedUsers.Any(b =>
                        b.TryGetProperty("id", out var bid) && bid.GetString() == id);

                    if (!isBlocked)
                    {
                        users.Add(new
                        {
                            id,
                            name = name ?? "Без имени",
                            avatar = avatar ?? "/images/default-avatar.png"
                        });
                    }
                }
            }

            return new JsonResult(users);
        }

        public IActionResult OnGetGetToken()
        {
            var token = HttpContext.Session.GetString("JWT_SECRET");
            return new JsonResult(new { token = token ?? "" });
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            var token = HttpContext.Session.GetString("JWT_SECRET");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }
    }
}
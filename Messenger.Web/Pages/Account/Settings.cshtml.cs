using Messenger.Core.DTOs.Users;
using Messenger.Core.Hubs;
using Messenger.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Messenger.Web.Pages.Account
{
    public class SettingsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IUserService _userService;

        public SettingsModel(IHttpClientFactory httpClientFactory, IHubContext<ChatHub> hubContext,
            IUserService userService)
        {
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _userService = userService;
        }

        [BindProperty]
        public UpdateUserProfileRequest Profile { get; set; } = new();

        [BindProperty]
        public IFormFile? AvatarFile { get; set; }

        [BindProperty]
        public bool DeleteAvatar { get; set; }

        public bool HasAvatar { get; private set; }
        public string? AvatarUrl { get; private set; }
        public JsonElement CurrentUserJson { get; private set; }
        public IReadOnlyList<JsonElement> BlockedUsers { get; private set; } = Array.Empty<JsonElement>();

        public async Task<IActionResult> OnGetAsync(bool? refreshed = null)
        {
            var token = HttpContext.Session.GetString("JWT_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Authorization/Authorization");
            }

            if (refreshed == true)
            {
                if (HasAvatar && AvatarUrl != null)
                {
                    AvatarUrl += (AvatarUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                }
            }

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var userIdString = HttpContext.Session.GetString("USER_ID");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return RedirectToPage("/Authorization/Authorization");
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return Page();

            var client = CreateClient();
            string? newAvatarUrl = null;

            try
            {
                if (DeleteAvatar)
                {
                    var deleteRes = await client.DeleteAsync("https://localhost:7001/api/users/delete-avatar");
                    if (!deleteRes.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("", "Не удалось удалить аватар");
                        await LoadDataAsync();
                        return Page();
                    }
                    newAvatarUrl = null;
                }

                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    if (AvatarFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "Файл не должен превышать 2 МБ");
                        await LoadDataAsync();
                        return Page();
                    }

                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                    if (!allowedTypes.Contains(AvatarFile.ContentType.ToLowerInvariant()))
                    {
                        ModelState.AddModelError("", "Неподдерживаемый формат");
                        await LoadDataAsync();
                        return Page();
                    }

                    using var content = new MultipartFormDataContent();
                    using var fileContent = new StreamContent(AvatarFile.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(AvatarFile.ContentType);
                    content.Add(fileContent, "avatarFile", AvatarFile.FileName);

                    var uploadRes = await client.PostAsync("https://localhost:7001/api/users/upload-avatar", content);
                    if (!uploadRes.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("", "Ошибка загрузки аватара");
                        await LoadDataAsync();
                        return Page();
                    }

                    var json = await uploadRes.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(json);
                    if (result.TryGetProperty("data", out var data) && data.TryGetProperty("avatarUrl", out var url))
                    {
                        newAvatarUrl = url.GetString();
                    }
                }

                var payload = new
                {
                    Profile.LastName,
                    Profile.FirstName,
                    Profile.MiddleName,
                    Profile.Login,
                    Profile.Phone,
                    Profile.Theme
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var updateUrl = "https://localhost:7001/api/users/update-profile";
                if (newAvatarUrl != null)
                {
                    updateUrl += $"?avatarUrl={Uri.EscapeDataString(newAvatarUrl)}";
                }

                var response = await client.PutAsync(updateUrl, jsonContent);
                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Ошибка сохранения профиля");
                    await LoadDataAsync();
                    return Page();
                }

                var displayName = $"{Profile.LastName} {Profile.FirstName.FirstOrDefault()}.".Trim();
                if (!string.IsNullOrEmpty(Profile.MiddleName))
                    displayName += Profile.MiddleName[0] + ".";

                if (newAvatarUrl != null || DeleteAvatar)
                {
                    await _hubContext.Clients.All.SendAsync("AvatarUpdated", new
                    {
                        userId = user.UserId,
                        avatarUrl = newAvatarUrl ?? "https://static.photos/people/200x200/4"
                    });
                }

                TempData["SuccessMessage"] = "Профиль успешно обновлён";
                return RedirectToPage(new { refreshed = true });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Произошла ошибка: " + ex.Message);
                await LoadDataAsync();
                return Page();
            }
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
                            Theme = data.TryGetProperty("account", out var acc) &&
                                    acc.TryGetProperty("theme", out var th) ? th.GetString() : "light"
                        };

                        string? avatarPath = null;
                        if (data.TryGetProperty("account", out var account) &&
                            account.TryGetProperty("avatar", out var avatarElement) &&
                            avatarElement.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(avatarElement.GetString()))
                        {
                            avatarPath = avatarElement.GetString();
                        }

                        HasAvatar = !string.IsNullOrEmpty(avatarPath);
                        AvatarUrl = !string.IsNullOrEmpty(avatarPath)
                            ? avatarPath + "?t=" + DateTime.Now.Ticks
                            : null;
                    }
                    else
                    {
                        CurrentUserJson = JsonDocument.Parse("{}").RootElement;
                        HasAvatar = false;
                        AvatarUrl = null;
                    }
                }
                else
                {
                    CurrentUserJson = JsonDocument.Parse("{}").RootElement;
                    HasAvatar = false;
                    AvatarUrl = null;
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки данных: {ex.Message}");
                CurrentUserJson = JsonDocument.Parse("{}").RootElement;
                BlockedUsers = Array.Empty<JsonElement>();
                HasAvatar = false;
                AvatarUrl = null;
            }
        }

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
            var token = HttpContext.Session.GetString("JWT_TOKEN");
            return new JsonResult(new { token = token ?? "" });
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var token = HttpContext.Session.GetString("JWT_TOKEN");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }
    }
}
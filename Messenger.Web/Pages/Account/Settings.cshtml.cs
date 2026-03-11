using Messenger.Core.DTOs.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Messenger.Web.Pages.Account
{
    [Authorize]
    public class SettingsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SettingsModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty]
        public UpdateUserProfileRequest Profile { get; set; } = new();

        [BindProperty]
        public IFormFile? AvatarFile { get; set; }

        [BindProperty]
        public bool DeleteAvatar { get; set; }

        public bool HasAvatar { get; private set; }
        public string? AvatarUrl { get; private set; }
        public UserProfileDto? CurrentUser { get; private set; }
        public List<BlockedUserDto> BlockedUsers { get; private set; } = new();

        public async Task OnGetAsync()
        {
            await LoadProfileAsync();
        }

        private async Task LoadProfileAsync()
        {
            var client = CreateAuthorizedClient();

            try
            {
                var profileRes = await client.GetAsync("api/users/info");
                if (profileRes.IsSuccessStatusCode)
                {
                    var json = await profileRes.Content.ReadAsStringAsync();
                    var root = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (root?.TryGetValue("data", out var dataObj) == true &&
                        dataObj is JsonElement dataElement &&
                        dataElement.ValueKind == JsonValueKind.Object)
                    {
                        var data = dataElement.GetRawText();
                        CurrentUser = JsonSerializer.Deserialize<UserProfileDto>(data);

                        if (CurrentUser != null)
                        {
                            Profile = new UpdateUserProfileRequest
                            {
                                LastName = CurrentUser.LastName ?? "",
                                FirstName = CurrentUser.FirstName ?? "",
                                MiddleName = CurrentUser.MiddleName,
                                Login = CurrentUser.Login ?? "",
                                Phone = CurrentUser.Phone,
                                Theme = CurrentUser.Account?.Theme ?? "light"
                            };

                            AvatarUrl = CurrentUser.Account?.Avatar;
                            HasAvatar = !string.IsNullOrWhiteSpace(AvatarUrl);
                            if (HasAvatar)
                                AvatarUrl += "?t=" + DateTimeOffset.Now.ToUnixTimeSeconds();
                        }
                    }
                }

                var blockedRes = await client.GetAsync("api/users/blocked");
                if (blockedRes.IsSuccessStatusCode)
                {
                    var json = await blockedRes.Content.ReadAsStringAsync();
                    var root = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (root?.TryGetValue("data", out var dataObj) == true &&
                        dataObj is JsonElement dataArray &&
                        dataArray.ValueKind == JsonValueKind.Array)
                    {
                        BlockedUsers = JsonSerializer.Deserialize<List<BlockedUserDto>>(dataArray.GetRawText()) ?? new();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadProfileAsync();
                return Page();
            }

            var client = CreateAuthorizedClient();

            try
            {
                string? newAvatarUrl = null;

                if (DeleteAvatar)
                {
                    await client.DeleteAsync("api/users/delete-avatar");
                    newAvatarUrl = null;
                }
                else if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    if (AvatarFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "Файл не должен превышать 2 МБ");
                        await LoadProfileAsync();
                        return Page();
                    }

                    var content = new MultipartFormDataContent();
                    var fileContent = new StreamContent(AvatarFile.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(AvatarFile.ContentType);
                    content.Add(fileContent, "avatarFile", AvatarFile.FileName);

                    var uploadRes = await client.PostAsync("api/users/upload-avatar", content);
                    if (uploadRes.IsSuccessStatusCode)
                    {
                        var json = await uploadRes.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                        if (result?.TryGetValue("data", out var dataObj) == true &&
                            dataObj is JsonElement data &&
                            data.TryGetProperty("avatarUrl", out var url))
                        {
                            newAvatarUrl = url.GetString();
                        }
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

                var jsonContent = JsonContent.Create(payload);
                var updateUrl = "api/users/update-profile";
                if (!string.IsNullOrEmpty(newAvatarUrl))
                    updateUrl += $"?avatarUrl={Uri.EscapeDataString(newAvatarUrl)}";

                var response = await client.PutAsync(updateUrl, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Ошибка обновления профиля");
                    await LoadProfileAsync();
                    return Page();
                }

                TempData["SuccessMessage"] = "Настройки успешно сохранены";
                return RedirectToPage(new { refreshed = true });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка сохранения: {ex.Message}");
                await LoadProfileAsync();
                return Page();
            }
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("ACCESS_TOKEN") ?? "";
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            client.BaseAddress = new Uri(_configuration["URL:API:HTTPS"] ?? "https://localhost:7001/");
            return client;
        }
    }

    public class UserProfileDto
    {
        public string? UserId { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? Login { get; set; }
        public string? Phone { get; set; }
        public AccountDto? Account { get; set; }
    }

    public class AccountDto
    {
        public string? Avatar { get; set; }
        public string? Theme { get; set; }
    }

    public class BlockedUserDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Login { get; set; }
        public string? Avatar { get; set; }
    }
}
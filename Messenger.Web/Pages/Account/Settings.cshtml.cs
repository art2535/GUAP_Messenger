using Messenger.Core.DTOs.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        public string? ErrorMessage { get; private set; }
        public string AccessToken { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            AccessToken = await HttpContext.GetTokenAsync("access_token") ?? "";
            await LoadProfileAsync();
        }

        private async Task LoadProfileAsync()
        {
            var client = await CreateAuthorizedClient();

            try
            {
                var profileRes = await client.GetAsync("api/users/info");
                if (profileRes.IsSuccessStatusCode)
                {
                    var json = await profileRes.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    var response = JsonSerializer.Deserialize<ApiResponse<UserProfileDto>>(json, options);

                    if (response?.IsSuccess == true && response.Data != null)
                    {
                        CurrentUser = response.Data;

                        Profile = new UpdateUserProfileRequest
                        {
                            LastName = CurrentUser.LastName ?? "",
                            FirstName = CurrentUser.FirstName ?? "",
                            MiddleName = CurrentUser.MiddleName ?? "",
                            Login = CurrentUser.Login ?? "",
                            Phone = CurrentUser.Phone ?? "",
                            Theme = CurrentUser.Account?.Theme ?? "light"
                        };

                        AvatarUrl = CurrentUser.Account?.Avatar;
                        HasAvatar = !string.IsNullOrWhiteSpace(AvatarUrl);
                        if (HasAvatar)
                        {
                            AvatarUrl += "?t=" + DateTimeOffset.Now.ToUnixTimeSeconds();
                        }
                    }
                    else
                    {
                        ErrorMessage = "Профиль не найден в ответе API (isSuccess или data отсутствует)";
                    }
                }
                else
                {
                    ErrorMessage = $"Ошибка API /users/info: {profileRes.StatusCode}";
                }

                var blockedRes = await client.GetAsync("api/users/blocked");
                if (blockedRes.IsSuccessStatusCode)
                {
                    var json = await blockedRes.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var response = JsonSerializer.Deserialize<ApiResponse<List<BlockedUserDto>>>(json, options);

                    if (response?.IsSuccess == true && response.Data != null)
                    {
                        BlockedUsers = response.Data;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Ошибка при загрузке настроек: " + ex.Message;
            }
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadProfileAsync();
                return Page();
            }

            var client = await CreateAuthorizedClient();

            try
            {
                string? newAvatarUrl = null;

                if (DeleteAvatar)
                {
                    var deleteRes = await client.DeleteAsync("api/users/delete-avatar");
                    if (!deleteRes.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("", "Не удалось удалить аватар");
                    }
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
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(AvatarFile.ContentType ?? "image/jpeg");
                    content.Add(fileContent, "avatarFile", AvatarFile.FileName);

                    var uploadRes = await client.PostAsync("api/users/upload-avatar", content);
                    if (uploadRes.IsSuccessStatusCode)
                    {
                        var json = await uploadRes.Content.ReadAsStringAsync();
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var result = JsonSerializer.Deserialize<ApiResponse<AvatarUploadResponse>>(json, options);

                        newAvatarUrl = result?.Data?.AvatarUrl;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Ошибка загрузки аватара");
                    }
                }

                var payload = new
                {
                    LastName = Profile.LastName,
                    FirstName = Profile.FirstName,
                    MiddleName = Profile.MiddleName,
                    Login = Profile.Login,
                    Phone = Profile.Phone,
                    Theme = Profile.Theme
                };

                var jsonContent = JsonContent.Create(payload);
                var updateUrl = "api/users/update-profile";
                if (!string.IsNullOrEmpty(newAvatarUrl))
                {
                    updateUrl += $"?avatarUrl={Uri.EscapeDataString(newAvatarUrl)}";
                }

                var response = await client.PutAsync(updateUrl, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Настройки успешно сохранены";
                    return RedirectToPage(new { refreshed = true });
                }
                else
                {
                    ModelState.AddModelError("", $"Ошибка обновления профиля: {response.StatusCode}");
                    var errContent = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка сохранения: {ex.Message}");
            }

            await LoadProfileAsync();
            return Page();
        }

        private async Task<HttpClient> CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = await HttpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            client.BaseAddress = new Uri(_configuration["URL:API:HTTPS"]);
            return client;
        }
    }
}
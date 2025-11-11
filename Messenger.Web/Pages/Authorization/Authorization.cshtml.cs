using Messenger.Core.DTOs.Auth;
using Messenger.Web.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace Messenger.Web.Pages.Authorization
{
    public class AuthorizationModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string BodyClass => "auth-page";

        [BindProperty]
        [Required(ErrorMessage = "Логин не может быть пустым")]
        public string Login { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Пароль не может быть пустым")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool IsRememberedMe { get; set; }

        public string? ErrorMessage { get; private set; } = string.Empty;

        public AuthorizationModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult OnGet()
        {
            var sessionToken = HttpContext.Session.GetString("JWT_SECRET");

            if (!string.IsNullOrEmpty(sessionToken))
            {
                return RedirectToPage("/Account/Chats");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            using (var httpClient = _httpClientFactory.CreateClient())
            {
                try
                {
                    var loginRequest = new LoginRequest
                    {
                        Login = Login,
                        Password = Password
                    };

                    var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync("https://localhost:7045/api/authorization/login", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        ErrorMessage = "Авторизация не прошла";
                        return Page();
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var userResponse = JsonSerializer.Deserialize<LoginResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (userResponse == null || string.IsNullOrEmpty(userResponse.Token))
                    {
                        ErrorMessage = "Ошибка: токен или пользователь не найден.";
                        return Page();
                    }

                    if (userResponse != null && !string.IsNullOrEmpty(userResponse.Token))
                    {
                        HttpContext.Session.SetString("JWT_SECRET", userResponse.Token);
                        HttpContext.Session.SetString("USER_EMAIL", loginRequest.Login);
                        HttpContext.Session.SetString("USER_ROLE", userResponse.Role);
                        HttpContext.Session.SetString("USER_ID", userResponse.UserId.ToString());

                        return RedirectToPage("/Account/Chats");
                    }

                    return Page();
                }
                catch (Exception ex)
                {
                    ErrorMessage = "Ошибка соединения с API: " + ex.Message;
                    return Page();
                }
            }
        }
    }
}

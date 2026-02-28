using Messenger.Core.DTOs.Auth;
using Messenger.Core.DTOs.Logins;
using Messenger.Web.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Messenger.Web.Pages.Authorization
{
    public class AuthorizationModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

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

        public AuthorizationModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public IActionResult OnGet()
        {
            var sessionToken = HttpContext.Session.GetString("JWT_TOKEN");

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

                    var response = await httpClient.PostAsync($"{_configuration["URL:API:HTTPS"]}/api/authorization/login", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        ErrorMessage = $"Авторизация не прошла: {response.StatusCode} — {errorContent}";
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
                        httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, userResponse.Token);

                        var logRequest = new CreateLoginRequest
                        {
                            Token = userResponse.Token,
                            IpAddress = GetLocalIPv4()
                        };

                        var logContent = new StringContent(JsonSerializer.Serialize(logRequest), Encoding.UTF8, "application/json");
                        var logResponse = await httpClient.PostAsync($"{_configuration["URL:API:HTTPS"]}/api/logins", logContent);

                        if (!logResponse.IsSuccessStatusCode)
                        {
                            ErrorMessage = "Ошибка записи лога входа";
                            return Page();
                        }

                        HttpContext.Session.SetString("JWT_TOKEN", userResponse.Token);
                        HttpContext.Session.SetString("USER_EMAIL", loginRequest.Login);
                        HttpContext.Session.SetString("USER_ROLE", userResponse.Role);
                        HttpContext.Session.SetString("USER_ID", userResponse.UserId.ToString());
                        HttpContext.Session.SetString("USER_NAME", userResponse.FullName ?? userResponse.UserName ?? loginRequest.Login);

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

        public static string GetLocalIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    var props = ni.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }

            return "IPv4 адрес компьютера не найден";
        }
    }
}

using Messenger.Core.DTOs.Auth;
using Messenger.Core.DTOs.Logins;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Messenger.Web.Pages.Authorization
{
    public class AuthorizationModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public string ErrorMessage { get; private set; } = string.Empty;

        public AuthorizationModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        
        public async Task<IActionResult> OnGetEtaLoginAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");

                var loginRequest = new CreateLoginRequest
                {
                    Token = accessToken,
                    IpAddress = GetLocalIPv4()
                };

                var response = await LoginAsync(loginRequest);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = "Ошибка записи входа в аккаунт";
                    return Page();
                }

                return RedirectToPage("/Account/Chats");
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Authorization/Authorization", "Callback")
            };

            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> OnGetCallbackAsync()
        {
            var externalId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            if (string.IsNullOrEmpty(externalId))
            {
                return RedirectToPage("/Authorization/Authorization", new { error = "Нет externalId" });
            }

            var firstName = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value ?? "ЕТА";
            var lastName = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")?.Value ?? "Пользователь";
            var email = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value ?? "";

            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");

                if (string.IsNullOrEmpty(accessToken))
                {
                    ErrorMessage = "Не удалось получить access token после авторизации";
                    return Page();
                }

                using var httpClient = _httpClientFactory.CreateClient();
                var request = new LoginEtaRequest
                {
                    ExternalId = externalId,
                    Email = email ?? "",
                    FirstName = firstName ?? "ЕТА",
                    LastName = lastName ?? "Пользователь",
                    MiddleName = "",
                    IpAddress = GetLocalIPv4(),
                    FakePasswordForInternalUse = $"external_{externalId.Substring(0, 8)}"
                };

                var response = await httpClient.PostAsJsonAsync(
                    $"{_configuration["URL:API:HTTPS"]}/api/authorization/external/callback", request);

                if (response.IsSuccessStatusCode)
                {
                    var loginRequest = new CreateLoginRequest
                    {
                        Token = accessToken,
                        IpAddress = GetLocalIPv4()
                    };

                    var loginResponse = await LoginAsync(loginRequest);

                    if (!loginResponse.IsSuccessStatusCode)
                    {
                        ErrorMessage = "Ошибка записи входа в аккаунт";
                        return Page();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API ОШИБКА: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            return RedirectToPage("/Account/Chats");
        }

        public async Task<HttpResponseMessage> LoginAsync(CreateLoginRequest request, CancellationToken token = default)
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri(_configuration["URL:API:HTTPS"]),
                Timeout = TimeSpan.FromSeconds(30)
            };

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", request.Token);

            return await client.PostAsJsonAsync("/api/logins", request, token);
        }

        private static string GetLocalIPv4()
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

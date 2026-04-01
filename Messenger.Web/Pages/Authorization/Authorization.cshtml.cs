using Messenger.Core.DTOs.Auth;
using Messenger.Core.DTOs.Logins;
using Messenger.Core.DTOs.UserStatuses;
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
        private readonly ILogger<AuthorizationModel> _logger;

        public string ErrorMessage { get; private set; } = string.Empty;

        public AuthorizationModel(IHttpClientFactory httpClientFactory, IConfiguration configuration,
            ILogger<AuthorizationModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
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
                    _logger.LogError("Ошибка записи входа в аккаунт");
                    return Page();
                }

                var userStatusRequest = new UpdateStatusRequest
                {
                    Online = true
                };

                using var client = new HttpClient
                {
                    BaseAddress = new Uri(_configuration["URL:API:HTTPS"]),
                    Timeout = TimeSpan.FromSeconds(30)
                };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var statusResponse = await client.PutAsJsonAsync(
                    $"{_configuration["URL:API:HTTPS"]}/api/userstatuses", userStatusRequest);

                if (!statusResponse.IsSuccessStatusCode)
                {
                    var error = await statusResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"Ошибка записи статуса онлайн: {statusResponse.StatusCode} - {error}");
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
                ErrorMessage = "Нет externalId";
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
                    _logger.LogError("Не удалось получить access token после авторизации");
                    ErrorMessage = "Не удалось получить access token";
                    return Page();
                }

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var request = new LoginEtaRequest
                {
                    ExternalId = externalId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    MiddleName = "",
                    IpAddress = GetLocalIPv4(),
                    FakePasswordForInternalUse = $"external_{externalId.Substring(0, 8)}"
                };

                var authResponse = await httpClient.PostAsJsonAsync(
                    $"{_configuration["URL:API:HTTPS"]}/api/authorization/external/callback", request);

                if (!authResponse.IsSuccessStatusCode)
                {
                    var error = await authResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"Ошибка внешней авторизации: {authResponse.StatusCode} - {error}");
                    ErrorMessage = "Ошибка авторизации в системе";
                    return Page();
                }

                var loginRequest = new CreateLoginRequest
                {
                    Token = accessToken,
                    IpAddress = GetLocalIPv4()
                };

                var loginResponse = await LoginAsync(loginRequest);

                if (!loginResponse.IsSuccessStatusCode)
                {
                    var error = await loginResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Ошибка записи входа: {loginResponse.StatusCode} - {error}");
                }

                var userStatusRequest = new UpdateStatusRequest
                {
                    Online = true
                };

                var statusResponse = await httpClient.PutAsJsonAsync(
                    $"{_configuration["URL:API:HTTPS"]}/api/userstatuses", userStatusRequest);

                if (!statusResponse.IsSuccessStatusCode)
                {
                    var error = await statusResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"Ошибка записи статуса онлайн: {statusResponse.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при входе через внешнего провайдера");
                ErrorMessage = "Внутренняя ошибка при входе";
                return Page();
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

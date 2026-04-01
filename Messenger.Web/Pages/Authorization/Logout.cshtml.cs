using Messenger.Core.DTOs.UserStatuses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;

namespace Messenger.Web.Pages.Authorization
{
    public class LogoutModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<LogoutModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = await HttpContext.GetTokenAsync("access_token");

            if (!string.IsNullOrEmpty(token))
            {
                using var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Patch, $"{_configuration["URL:API:HTTPS"]}/api/logins");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                try
                {
                    var response = await client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Ошибка API при выходе: {response.StatusCode} - {error}");
                    }

                    var userStatusRequest = new UpdateStatusRequest
                    {
                        Online = false
                    };

                    var statusRequest = new HttpRequestMessage(HttpMethod.Put, 
                        $"{_configuration["URL:API:HTTPS"]}/api/userstatuses")
                    {
                        Content = JsonContent.Create(userStatusRequest)
                    };
                    statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var userStatusResponse = await client.SendAsync(statusRequest);

                    if (!userStatusResponse.IsSuccessStatusCode)
                    {
                        var error = await userStatusResponse.Content.ReadAsStringAsync();
                        _logger.LogError($"Ошибка записи статуса при выходе: {userStatusResponse.StatusCode} - {error}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Не удалось связаться с API при выходе: " + ex.Message);
                }
            }

            HttpContext.Session.Clear();

            return RedirectToPage("/Authorization/Authorization");
        }
    }
}
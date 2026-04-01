using Messenger.Core.DTOs.UserStatuses;
using Messenger.Core.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Messenger.Web.Pages.Authorization
{
    public class LogoutModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LogoutModel> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public string ErrorMessage { get; set; } = string.Empty;

        public LogoutModel(IHttpClientFactory httpClientFactory, IConfiguration configuration,
            ILogger<LogoutModel> logger, IHubContext<ChatHub> hubContext)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _hubContext = hubContext;
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

                    if (userStatusResponse.IsSuccessStatusCode)
                    {
                        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                     ?? User.FindFirst("sub")?.Value;

                        if (Guid.TryParse(userIdStr, out Guid userId))
                        {
                            try
                            {
                                await _hubContext.Clients.All.SendAsync("UserOnlineStatusChanged", new
                                {
                                    userId = userId.ToString(),
                                    isOnline = true,
                                    lastActivity = DateTime.UtcNow
                                });

                                await _hubContext.Clients.User(userId.ToString()).SendAsync("UserOnlineStatusChanged", new
                                {
                                    userId = userId.ToString(),
                                    isOnline = true,
                                    lastActivity = DateTime.UtcNow
                                });

                                _logger.LogInformation("SignalR уведомление о входе отправлено для пользователя {UserId}", userId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("Не удалось отправить SignalR уведомление о входе: {Message}", ex.Message);
                            }
                        }
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
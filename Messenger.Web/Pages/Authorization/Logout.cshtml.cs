using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;

namespace Messenger.Web.Pages.Authorization
{
    public class LogoutModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LogoutModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = HttpContext.Session.GetString("JWT_SECRET");

            if (!string.IsNullOrEmpty(token))
            {
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Patch, "https://localhost:7001/api/logins");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                try
                {
                    var response = await client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Ошибка API при выходе: {response.StatusCode} — {error}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Не удалось связаться с API при выходе: " + ex.Message);
                }
            }

            HttpContext.Session.Clear();

            return RedirectToPage("/Authorization/Authorization");
        }
    }
}
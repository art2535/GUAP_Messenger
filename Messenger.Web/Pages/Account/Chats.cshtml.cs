using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Messenger.Web.Pages.Account
{
    public class ChatsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public string? UserId { get; set; }
        public string? UserName { get; set; } = string.Empty;
        public string? UserRole { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public ChatsModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            bool isOidcAuthenticated = User.Identity?.IsAuthenticated == true;

            if (isOidcAuthenticated)
            {
                UserName = User.FindFirstValue("name")
                        ?? User.FindFirstValue("preferred_username")
                        ?? "Пользователь ЕТА";

                UserId = User.FindFirstValue("sub");

                UserRole = User.FindFirstValue("role")
                        ?? User.FindFirstValue("roles")
                        ?? "Пользователь";

                var accessToken = await HttpContext.GetTokenAsync("access_token");

                HttpContext.Session.SetString("ACCESS_TOKEN", accessToken ?? "");
                HttpContext.Session.SetString("USER_ID", UserId ?? "");
                HttpContext.Session.SetString("USER_NAME", UserName);
                HttpContext.Session.SetString("USER_ROLE", UserRole);
            }
            else
            {
                var token = HttpContext.Session.GetString("JWT_TOKEN");
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToPage("/Authorization/Authorization");
                }

                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    HttpContext.Session.Clear();
                    return RedirectToPage("/Authorization/Authorization");
                }

                var jwtToken = handler.ReadJwtToken(token);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    HttpContext.Session.Clear();
                    return RedirectToPage("/Authorization/Authorization");
                }

                UserId = HttpContext.Session.GetString("USER_ID");
                UserName = HttpContext.Session.GetString("USER_NAME") ?? "Локальный пользователь";
                UserRole = HttpContext.Session.GetString("USER_ROLE") ?? "Пользователь";
            }

            return Page();
        }
    }
}

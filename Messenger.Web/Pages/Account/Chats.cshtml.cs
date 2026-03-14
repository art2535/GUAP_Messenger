using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Messenger.Web.Pages.Account
{
    public class ChatsModel : PageModel
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; } = string.Empty;
        public string? UserRole { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            bool isOidcAuthenticated = User.Identity?.IsAuthenticated == true;

            if (User.Identity?.IsAuthenticated == true)
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

            return Page();
        }
    }
}

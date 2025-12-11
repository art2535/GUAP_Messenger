using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;

namespace Messenger.Web.Pages.Account
{
    public class ChatsModel : PageModel
    {
        public string? UserId { get; set; }
        public string? JwtToken { get; set; }
        public string? UserName { get; set; } = string.Empty;
        public string? UserRole { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            var token = HttpContext.Session.GetString("JWT_TOKEN");

            if (string.IsNullOrEmpty(token))
                return RedirectToPage("/Authorization/Authorization");

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Authorization/Authorization");
            }

            UserId = HttpContext.Session.GetString("USER_ID");
            UserName = HttpContext.Session.GetString("USER_EMAIL");
            UserRole = HttpContext.Session.GetString("USER_ROLE");

            return Page();
        }
    }
}

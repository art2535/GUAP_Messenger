using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
            JwtToken = HttpContext.Session.GetString("JWT_SECRET");
            UserId = HttpContext.Session.GetString("USER_ID");
            UserName = HttpContext.Session.GetString("USER_EMAIL");
            UserRole = HttpContext.Session.GetString("USER_ROLE");

            if (string.IsNullOrEmpty(JwtToken))
                return RedirectToPage("/Authorization/Authorization");

            return Page();
        }
    }
}

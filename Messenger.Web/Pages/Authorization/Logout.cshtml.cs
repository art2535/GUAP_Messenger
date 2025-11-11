using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Messenger.Web.Pages.Authorization
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnPost()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("JWT_SECRET");
            return RedirectToPage("/Authorization/Authorization");
        }
    }
}

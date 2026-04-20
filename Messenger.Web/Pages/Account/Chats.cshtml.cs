using Messenger.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Messenger.Web.Pages.Account
{
    public class ChatsModel : PageModel
    {
        private readonly IUserService _userService;

        public string? UserId { get; set; }
        public string? UserName { get; set; } = string.Empty;
        public string? UserRole { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public ChatsModel(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> OnGetAsync(bool tokenSaved = false)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var externalId = User.FindFirstValue("sub")
                              ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                              ?? User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                              ?? User.FindFirst("sub")?.Value;

                var user = await _userService.GetUserByExternalIdAsync(externalId);
                if (user != null)
                {
                    UserId = user.UserId.ToString();
                }
                else
                {
                    UserId = externalId;
                }

                UserName = User.FindFirstValue("name")
                        ?? User.FindFirstValue("preferred_username")
                        ?? "Пользователь";

                UserRole = User.FindFirstValue("role")
                        ?? User.FindFirstValue("roles")
                        ?? "Пользователь";

                var accessToken = await HttpContext.GetTokenAsync("access_token");
                HttpContext.Session.SetString("ACCESS_TOKEN", accessToken ?? "");
                HttpContext.Session.SetString("USER_ID", UserId ?? ""); 
                HttpContext.Session.SetString("USER_NAME", UserName);
                HttpContext.Session.SetString("USER_ROLE", UserRole);
            }

            ViewData["TokenSaved"] = tokenSaved;

            return Page();
        }
    }
}

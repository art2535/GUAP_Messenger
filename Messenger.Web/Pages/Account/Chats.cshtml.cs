using Messenger.Core.Models;
using Messenger.Web.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Messenger.Web.Pages.Account
{
    [Authorize]
    public class ChatsModel : PageModel
    {
        private readonly ApiHelper _api;

        public string? UserId { get; set; }
        public string? UserName { get; set; } = string.Empty;
        public string? UserRole { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool TokenSaved { get; set; }

        public string ApiBaseUrl { get; private set; } = string.Empty;
        public string HubUrl { get; private set; } = string.Empty;

        public ChatsModel(ApiHelper api)
        {
            _api = api;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated != true && !TokenSaved)
                return RedirectToPage("/Authorization/Authorization");

            if (User.Identity?.IsAuthenticated == true)
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");

                var externalId = User.FindFirstValue("sub")
                              ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                              ?? User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                              ?? User.FindFirst("sub")?.Value;

                var user = await _api.GetAsync<User>($"users/{Uri.EscapeDataString(externalId ?? "")}", accessToken);

                UserId = user?.UserId.ToString() ?? externalId;

                UserName = User.FindFirstValue("name")
                        ?? User.FindFirstValue("preferred_username")
                        ?? "Пользователь";

                UserRole = User.FindFirstValue("role")
                        ?? User.FindFirstValue("roles")
                        ?? "Пользователь";

                if (!string.IsNullOrEmpty(accessToken))
                    HttpContext.Session.SetString("ACCESS_TOKEN", accessToken);
            }

            HttpContext.Session.SetString("USER_ID", UserId ?? "");
            HttpContext.Session.SetString("USER_NAME", UserName ?? "");
            HttpContext.Session.SetString("USER_ROLE", UserRole ?? "");

            ApiBaseUrl = _api.GetApiUrl();
            HubUrl = $"{ApiBaseUrl}/hubs/chat";

            return Page();
        }
    }
}
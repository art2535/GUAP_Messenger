using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Messenger.Core.Interfaces;
using Messenger.Core.Models;

namespace Messenger.API.Services
{
    public static class UserValidationService
    {
        public static async Task<(User? User, IActionResult? Error)> GetCurrentUserOrErrorAsync(
            ClaimsPrincipal user, IUserService userService)
        {
            var externalId = user.FindFirst("sub")?.Value
                          ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (string.IsNullOrEmpty(externalId))
            {
                return (null, new UnauthorizedObjectResult("Не найден внешний идентификатор (sub / nameidentifier)"));
            }

            var dbUser = await userService.GetUserByExternalIdAsync(externalId);
            if (dbUser == null)
            {
                return (null, new BadRequestObjectResult("Пользователь с указанным внешним идентификатором не найден в системе"));
            }

            return (dbUser, null);
        }

        public static async Task<User> GetCurrentUserAsync(ClaimsPrincipal user, IUserService userService)
        {
            var (dbUser, error) = await GetCurrentUserOrErrorAsync(user, userService);

            if (error != null)
            {
                throw new InvalidOperationException("Не удалось получить текущего пользователя: " +
                    (error as ObjectResult)?.Value?.ToString() ?? "Неизвестная ошибка");
            }

            return dbUser!;
        }
    }
}
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Messenger.API.Providers
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst("sub")?.Value.ToLowerInvariant()
              ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value.ToLowerInvariant();
        }
    }
}

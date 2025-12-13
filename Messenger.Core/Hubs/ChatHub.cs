using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Messenger.Core.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        public async Task JoinChat(Guid chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
        }

        public async Task LeaveChat(Guid chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                await Clients.All.SendAsync("UserOnline", userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Clients.All.SendAsync("UserOffline", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task NotifyBlocked(string blockedUserId)
        {
            await Clients.User(blockedUserId).SendAsync("UserBlockedMe", Context.UserIdentifier);
        }

        public async Task NotifyUnblocked(string unblockedUserId)
        {
            await Clients.User(unblockedUserId).SendAsync("UserUnblockedMe", Context.UserIdentifier);
        }

        public async Task NotifyParticipantRemoved(Guid chatId, Guid removedUserId)
        {
            await Clients.Group(chatId.ToString()).SendAsync("ParticipantRemoved", new
            {
                chatId = chatId,
                userId = removedUserId
            });

            await Clients.User(removedUserId.ToString()).SendAsync("YouWereRemovedFromChat", chatId);
        }

        public async Task NotifyParticipantAdded(Guid chatId, Guid addedUserId, object userInfo)
        {
            await Clients.Group(chatId.ToString()).SendAsync("ParticipantAdded", new
            {
                chatId,
                user = userInfo
            });
        }

        public async Task NotifyChatDeleted(Guid chatId)
        {
            await Clients.Group(chatId.ToString()).SendAsync("ChatDeleted", chatId);
        }

        public async Task NotifyChatUpdated(Guid chatId, string newName)
        {
            await Clients.Group(chatId.ToString()).SendAsync("ChatUpdated", new
            {
                chatId,
                name = newName
            });
        }

        public async Task NotifyAvatarUpdated(string userId, string newAvatarUrl)
        {
            await Clients.All.SendAsync("AvatarUpdated", new
            {
                userId,
                avatarUrl = newAvatarUrl
            });
        }

        public async Task NotifyProfileNameUpdated(string userId, string newDisplayName)
        {
            await Clients.All.SendAsync("ProfileNameUpdated", new
            {
                userId,
                displayName = newDisplayName
            });
        }

        public async Task NotifyProfileUpdated(string userId, string? newAvatarUrl, string newDisplayName)
        {
            await Clients.All.SendAsync("ProfileUpdated", new
            {
                userId,
                avatarUrl = newAvatarUrl,
                displayName = newDisplayName
            });
        }
    }
}

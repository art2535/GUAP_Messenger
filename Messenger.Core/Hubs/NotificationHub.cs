using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Messenger.Core.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;

        public NotificationHub(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Notifications_{userId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Notifications_{userId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotification(Guid userId, string text)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Text = text,
                CreationDate = DateTime.UtcNow,
                Read = false
            };

            var cancellationToken = Context.GetHttpContext()?.RequestAborted ?? CancellationToken.None;

            await _notificationService.CreateNotificationAsync(userId, text, cancellationToken);
            await Clients.Group($"Notifications_{userId}").SendAsync("ReceiveNotification", notification);
        }
    }
}

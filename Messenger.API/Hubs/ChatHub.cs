using Messenger.Core.Interfaces;
using Messenger.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Messenger.API.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IMessageStatusService _messageStatusService;
        private readonly IReactionService _reactionService;

        public ChatHub(IMessageService messageService, IMessageStatusService messageStatusService,
            IReactionService reactionService)
        {
            _messageService = messageService;
            _messageStatusService = messageStatusService;
            _reactionService = reactionService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(Guid chatId, Guid receiverId, string content, bool hasAttachments)
        {
            var senderId = Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var message = new Message
            {
                MessageId = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                RecipientId = receiverId,
                MessageText = content,
                HasAttachments = hasAttachments,
                SendTime = DateTime.UtcNow
            };

            var cancellationToken = Context.GetHttpContext()?.RequestAborted ?? CancellationToken.None;

            await _messageService.SendMessageAsync(chatId, senderId, receiverId, content, hasAttachments, cancellationToken);

            await Clients.Group($"User_{senderId}").SendAsync("ReceiveMessage", message);
            await Clients.Group($"User_{receiverId}").SendAsync("ReceiveMessage", message);

            var deliveredStatus = new MessageStatus
            {
                MessageId = message.MessageId,
                UserId = receiverId,
                Status = "Delivered",
                ChangeDate = DateTime.UtcNow
            };
            await _messageStatusService.AddOrUpdateStatusAsync(deliveredStatus, cancellationToken);
            await Clients.Group($"User_{senderId}").SendAsync("MessageStatusUpdated", deliveredStatus);
        }

        public async Task UpdateMessageStatus(Guid messageId, string status)
        {
            var userId = Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var messageStatus = new MessageStatus
            {
                MessageId = messageId,
                UserId = userId,
                Status = status,
                ChangeDate = DateTime.UtcNow
            };

            var cancellationToken = Context.GetHttpContext()?.RequestAborted ?? CancellationToken.None;

            await _messageStatusService.AddOrUpdateStatusAsync(messageStatus, cancellationToken);

            var message = await _messageService.GetMessagesAsync(messageStatus.MessageId, cancellationToken);
            var senderId = message.FirstOrDefault()?.SenderId;
            if (senderId != null)
            {
                await Clients.Group($"User_{senderId}").SendAsync("MessageStatusUpdated", messageStatus);
            }
        }

        public async Task AddReaction(Guid messageId, string reactionType)
        {
            var userId = Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var reaction = new Reaction
            {
                ReactionId = Guid.NewGuid(),
                MessageId = messageId,
                UserId = userId,
                ReactionType = reactionType
            };

            var cancellationToken = Context.GetHttpContext()?.RequestAborted ?? CancellationToken.None;

            await _reactionService.AddReactionAsync(reaction, cancellationToken);

            var message = await _messageService.GetMessagesAsync(messageId, cancellationToken);
            var chatId = message.FirstOrDefault()?.ChatId;
            if (chatId != null)
            {
                await Clients.Group($"Chat_{chatId}").SendAsync("ReactionAdded", reaction);
            }
        }

        public async Task JoinChat(Guid chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
        }

        public async Task LeaveChat(Guid chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
        }
    }
}

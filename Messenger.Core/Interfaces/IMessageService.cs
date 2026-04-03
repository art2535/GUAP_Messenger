using Messenger.Core.DTOs;
using Messenger.Core.DTOs.Messages;
using Messenger.Core.Models;
using Microsoft.AspNetCore.Http;

namespace Messenger.Core.Interfaces
{
    public interface IMessageService
    {
        Task<ServiceResult<Message>> SendMessageAsync(Guid chatId, Guid senderId,
            string? content, bool hasAttachments, IFormFile[]? files = null, CancellationToken token = default);
        Task<IEnumerable<Message>> GetMessagesAsync(Guid chatId, CancellationToken token = default);
        Task<Message?> GetMessageByIdAsync(Guid chatId, Guid messageId, CancellationToken token = default);
        Task DeleteMessageAsync(Guid messageId, CancellationToken token = default);
        Task UpdateMessageAsync(Message message, CancellationToken token = default);
        Task<List<MessageDto>> SearchMessagesAsync(Guid chatId, string query, CancellationToken token = default);
    }
}

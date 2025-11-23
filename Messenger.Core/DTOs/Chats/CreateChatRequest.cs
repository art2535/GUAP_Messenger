using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.Chats
{
    public class CreateChatRequest
    {
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public List<Guid> UserIds { get; set; } = new();
    }
}

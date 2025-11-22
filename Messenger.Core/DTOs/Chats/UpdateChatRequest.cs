using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.Chats
{
    public class UpdateChatRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}

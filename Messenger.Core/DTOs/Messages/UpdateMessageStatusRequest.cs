using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.Messages
{
    public class UpdateMessageStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}

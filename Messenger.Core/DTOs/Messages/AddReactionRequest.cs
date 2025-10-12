using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.Messages
{
    public class AddReactionRequest
    {
        [Required]
        public string ReactionType { get; set; } = string.Empty;
    }
}

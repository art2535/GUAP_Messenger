using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.Reactions
{
    public class CreateReactionRequest
    {
        [Required]
        public string ReactionType { get; set; } = string.Empty;
    }
}

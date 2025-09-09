using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Реакции"
    /// </summary>
    public class MessageReaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid MessageId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(30)]
        public string ReactionType { get; set; } = string.Empty;
        public Message? Message { get; set; }
        public User? User { get; set; }
    }
}
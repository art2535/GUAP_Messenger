using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Реакции"
    /// </summary>
    public class MessageReaction
    {
        [Key]
        public Guid MessageReactionId { get; set; }

        [ForeignKey(nameof(Message))]
        public Guid MessageId { get; set; }

        [JsonIgnore]
        public Message? Message { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        [Required, StringLength(30)]
        public string ReactionType { get; set; } = string.Empty;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Сообщения"
    /// </summary>
    public class Message
    {
        [Key]
        public Guid MessageId { get; set; }

        [ForeignKey(nameof(Sender))]
        public Guid SenderId { get; set; }

        [JsonIgnore]
        public User? Sender { get; set; }

        [ForeignKey(nameof(Recipient))]
        public Guid RecipientId { get; set; }

        [JsonIgnore]
        public User? Recipient { get; set; }

        [ForeignKey(nameof(Chat))]
        public Guid ChatId { get; set; }

        [JsonIgnore]
        public Chat? Chat { get; set; }

        [Required]
        public string MessageText { get; set; } = string.Empty;

        public bool HasAttachments { get; set; }

        [Required]
        public DateTime SendTime { get; set; }
    }
}
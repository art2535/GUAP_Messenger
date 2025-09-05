using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Статусы сообщений"
    /// </summary>
    public class MessageStatus
    {
        [ForeignKey(nameof(Message))]
        public Guid MessageId { get; set; }

        [JsonIgnore]
        public Message? Message { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = string.Empty;

        [Required]
        public DateTime ChangeDate { get; set; }
    }
}
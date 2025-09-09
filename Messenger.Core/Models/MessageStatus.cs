using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Статусы сообщений"
    /// </summary>
    public class MessageStatus
    {
        [Key]
        public Guid MessageId { get; set; }

        [Key]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        [Required]
        public DateTime UpdatedAt { get; set; }
        public Message? Message { get; set; }
        public User? User { get; set; }
    }
}
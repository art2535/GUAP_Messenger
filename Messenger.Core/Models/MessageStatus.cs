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
        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public Message? Message { get; set; }
        public User? User { get; set; }
    }
}
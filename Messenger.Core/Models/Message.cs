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
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public Guid ChatId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool HasAttachments { get; set; }
        public DateTime SentAt { get; set; }
        public User? Sender { get; set; }
        public User? Receiver { get; set; }
        public Chat? Chat { get; set; }
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public List<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public List<MessageStatus> Statuses { get; set; } = new List<MessageStatus>();
    }
}
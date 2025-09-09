using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Сообщения"
    /// </summary>
    public class Message
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SenderId { get; set; }

        [Required]
        public Guid ReceiverId { get; set; }

        [Required]
        public Guid ChatId { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        [Required]
        public bool HasAttachments { get; set; }

        [Required]
        public DateTime SentAt { get; set; }
        public User? Sender { get; set; }
        public User? Receiver { get; set; }
        public Chat? Chat { get; set; }
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public List<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public List<MessageStatus> Statuses { get; set; } = new List<MessageStatus>();
    }
}
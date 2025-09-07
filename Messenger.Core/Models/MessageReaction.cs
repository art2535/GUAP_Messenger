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
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public string ReactionType { get; set; } = string.Empty;
        public Message? Message { get; set; }
        public User? User { get; set; }
    }
}
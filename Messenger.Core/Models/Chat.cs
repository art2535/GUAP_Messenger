using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Чаты"
    /// </summary>
    public class Chat
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Guid CreatorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public User? Creator { get; set; }
        public List<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
        public List<Message> Messages { get; set; } = new List<Message>();
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Участники чата"
    /// </summary>
    public class ChatParticipant
    {
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public Chat? Chat { get; set; }
        public User? User { get; set; }
    }
}
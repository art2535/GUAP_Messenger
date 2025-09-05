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
        [ForeignKey(nameof(Chat))]
        public Guid ChatId { get; set; }

        [JsonIgnore]
        public Chat? Chat { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [JsonIgnore] 
        public User? User { get; set; }

        [StringLength(20)]
        public string Role { get; set; } = "участник";

        [Required]
        public DateTime DateOfEntry { get; set; }
    }
}
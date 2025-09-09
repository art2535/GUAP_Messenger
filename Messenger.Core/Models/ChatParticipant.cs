using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Участники чата"
    /// </summary>
    public class ChatParticipant
    {
        [Key]
        public Guid ChatId { get; set; }

        [Key]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        [Required]
        public DateTime JoinedAt { get; set; }
        public Chat? Chat { get; set; }
        public User? User { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Чаты"
    /// </summary>
    public class Chat
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty;

        [Required]
        public Guid CreatorId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
        public User? Creator { get; set; }
        public List<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
        public List<Message> Messages { get; set; } = new List<Message>();
    }
}
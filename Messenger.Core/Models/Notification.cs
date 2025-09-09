using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Уведомления"
    /// </summary>
    public class Notification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public bool IsRead { get; set; }
        public User? User { get; set; }
    }
}
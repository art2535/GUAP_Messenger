using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Чёрный список"
    /// </summary>
    public class BlackList
    {
        [Key]
        public Guid UserId { get; set; }

        [Key]
        public Guid BlockedUserId { get; set; }

        [Required]
        public DateTime BlockedAt { get; set; }
        public User? User { get; set; }
        public User? BlockedUser { get; set; }
    }
}
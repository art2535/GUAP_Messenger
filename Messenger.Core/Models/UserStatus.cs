using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Статусы пользователей"
    /// </summary>
    public class UserStatus
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        public bool IsOnline { get; set; }
        public DateTime? LastActivity { get; set; }
        public User? User { get; set; }
    }
}
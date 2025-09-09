using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Входы"
    /// </summary>
    public class LoginSession
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string IpAddress { get; set; } = string.Empty;

        [Required]
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }

        [Required]
        public bool IsActive { get; set; }
        public User? User { get; set; }
    }
}
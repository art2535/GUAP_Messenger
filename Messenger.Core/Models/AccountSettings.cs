using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Настройки аккаунта"
    /// </summary>
    public class AccountSettings
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AccountId { get; set; }
        public string? Avatar { get; set; }
        public string? Theme { get; set; }
        public User? User { get; set; }
    }
}
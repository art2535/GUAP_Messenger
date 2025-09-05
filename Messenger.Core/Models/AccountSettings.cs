using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Настройки аккаунта"
    /// </summary>
    public class AccountSettings
    {
        [Key]
        public Guid SettingsId { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        public string? Avatar { get; set; }

        [StringLength(15)]
        public string Theme { get; set; } = "светлая";
    }
}
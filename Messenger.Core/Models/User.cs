using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Пользователи"
    /// </summary>
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(AccountSettings))]
        public Guid AccountSettingsId { get; set; }

        [JsonIgnore]
        public AccountSettings? AccountSettings { get; set; }

        [Required, StringLength(50)]
        public string Surname { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? MiddleName { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public DateTime RegisterDate { get; set; }

        [Required, StringLength(20)]
        public string Login { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(18)]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
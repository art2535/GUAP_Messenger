using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Входы"
    /// </summary>
    public class LoginSession
    {
        [Key]
        public Guid SessionId { get; set; }

        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        [Required, StringLength(100)]
        public string Token { get; set; } = string.Empty;

        [Required, StringLength(15)]
        public string IPAddress { get; set; } = string.Empty;

        [Required]
        public DateTime EnterTime { get; set; }

        public DateTime? ExitTime { get; set; }

        public bool IsActive { get; set; }
    }
}
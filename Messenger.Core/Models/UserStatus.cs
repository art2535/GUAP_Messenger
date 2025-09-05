using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Статусы пользователей"
    /// </summary>
    public class UserStatus
    {
        [Key, ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        public bool IsOnline { get; set; }

        public DateTime? LastActiveDate { get; set; }
    }
}
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
        public Guid UserId { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastActivity { get; set; }
        public User? User { get; set; }
    }
}
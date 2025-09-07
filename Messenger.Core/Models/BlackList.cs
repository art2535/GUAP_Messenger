using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Чёрный список"
    /// </summary>
    public class BlackList
    {
        public Guid UserId { get; set; }
        public Guid BlockedUserId { get; set; }
        public DateTime BlockedAt { get; set; }
        public User? User { get; set; }
        public User? BlockedUser { get; set; }
    }
}
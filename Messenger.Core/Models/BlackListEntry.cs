using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Чёрный список"
    /// </summary>
    public class BlackListEntry
    {
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        [ForeignKey(nameof(BlockedUser))]
        public Guid BlockedUserId { get; set; }

        [JsonIgnore]
        public User? BlockedUser { get; set; }

        public DateTime BlockedDate { get; set; }
    }
}
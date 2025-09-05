using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Чаты"
    /// </summary>
    public class Chat
    {
        [Key]
        public Guid ChatId { get; set; }

        [Required, StringLength(100)]
        public string ChatName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Type { get; set; } = string.Empty;

        [ForeignKey(nameof(User)]
        public Guid UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        [Required]
        public DateTime CreateDate { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Вложения"
    /// </summary>
    public class Attachment
    {
        [Key]
        public Guid AttachmentId { get; set; }

        [ForeignKey(nameof(Message))]
        public Guid MessageId { get; set; }

        [JsonIgnore]
        public Message? Message { get; set; }

        [Required, StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ContentType { get; set; }

        public int? Size { get; set; }

        [Required]
        public string Url { get; set; } = string.Empty;
    }
}
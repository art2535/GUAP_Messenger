using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Вложения"
    /// </summary>
    public class Attachment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid MessageId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FileType { get; set; }
        public int? FileSize { get; set; }

        [Required]
        public string Url { get; set; } = string.Empty;
        public Message? Message { get; set; }
    }
}
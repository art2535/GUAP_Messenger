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
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FileType { get; set; }
        public int? FileSize { get; set; }
        public string Url { get; set; } = string.Empty;
        public Message? Message { get; set; }
    }
}
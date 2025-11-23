namespace Messenger.Core.DTOs.Messages
{
    public class MessageDto
    {
        public Guid MessageId { get; set; }
        public Guid ChatId { get; set; }
        public Guid SenderId { get; set; }
        public string? MessageText { get; set; }
        public DateTime SentAt { get; set; }
        public string Status { get; set; } = "Sent";
        public List<AttachmentDto> Attachments { get; set; } = new();
    }

    public class AttachmentDto
    {
        public Guid AttachmentId { get; set; }
        public string FileName { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public long SizeInBytes { get; set; }
        public string Url { get; set; } = null!;
    }
}

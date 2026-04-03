namespace Messenger.Core.Messages
{
    public record ChatMessageSent
    {
        public Guid MessageId { get; init; }
        public Guid ChatId { get; init; }
        public int SequenceNumber { get; init; }
        public Guid SenderId { get; init; }
        public string MessageText { get; init; } = string.Empty;
        public DateTime SentAt { get; init; }
        public bool HasAttachments { get; init; }
    }
}

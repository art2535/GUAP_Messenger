namespace Messenger.Core.DTOs.Broadcasts
{
    public class MarkAsReadResponse
    {
        public bool Success { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}

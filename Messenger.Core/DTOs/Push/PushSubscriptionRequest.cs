namespace Messenger.Core.DTOs.Push
{
    public class PushSubscriptionRequest
    {
        public string Endpoint { get; set; } = string.Empty;

        public string P256dh { get; set; } = string.Empty;

        public string Auth { get; set; } = string.Empty;
    }
}
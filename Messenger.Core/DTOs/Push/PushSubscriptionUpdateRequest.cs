namespace Messenger.Core.DTOs.Push
{
    public class PushSubscriptionUpdateRequest
    {
        public bool PushEnabled { get; set; } = true;

        public bool NotifyMessages { get; set; } = true;

        public bool NotifyGroupChats { get; set; } = true;

        public bool NotifyMentions { get; set; } = true;
    }
}

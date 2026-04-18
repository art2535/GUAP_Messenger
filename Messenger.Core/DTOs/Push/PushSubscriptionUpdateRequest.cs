using Swashbuckle.AspNetCore.Annotations;

namespace Messenger.Core.DTOs.Push
{
    public class PushSubscriptionUpdateRequest
    {
        [SwaggerSchema("Включены ли push-уведомления в целом")]
        public bool PushEnabled { get; set; } = true;

        [SwaggerSchema("Уведомления о личных сообщениях")]
        public bool NotifyMessages { get; set; } = true;

        [SwaggerSchema("Уведомления в групповых чатах")]
        public bool NotifyGroupChats { get; set; } = true;

        [SwaggerSchema("Уведомления об упоминаниях")]
        public bool NotifyMentions { get; set; } = true;
    }
}

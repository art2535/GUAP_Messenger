using Swashbuckle.AspNetCore.Annotations;

namespace Messenger.Core.DTOs.Push
{
    public class PushSubscriptionRequest
    {
        [SwaggerSchema("Endpoint push-сервиса")]
        public string Endpoint { get; set; } = string.Empty;

        [SwaggerSchema("Ключ p256dh (Elliptic Curve Diffie-Hellman)")]
        public string P256dh { get; set; } = string.Empty;

        [SwaggerSchema("Ключ auth")]
        public string Auth { get; set; } = string.Empty;
    }
}
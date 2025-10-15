using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.Notifications
{
    public class CreateNotificationRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;
    }
}

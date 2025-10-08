using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs
{
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }
}

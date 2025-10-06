using System.ComponentModel.DataAnnotations;

namespace Messenger.API.DTOs
{
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}

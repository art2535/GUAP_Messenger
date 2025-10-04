using System.ComponentModel.DataAnnotations;

namespace Messenger.API.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string Login { get; set; }

        [Required]
        public string Password { get; set; }
    }
}

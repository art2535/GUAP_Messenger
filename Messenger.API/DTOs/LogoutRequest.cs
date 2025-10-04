using System.ComponentModel.DataAnnotations;

namespace Messenger.API.DTOs
{
    public class LogoutRequest
    {
        [Required]
        public string Login { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Token { get; set; }
    }
}

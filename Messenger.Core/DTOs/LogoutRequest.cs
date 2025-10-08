using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs
{
    public class LogoutRequest
    {
        [Required]
        public string Login { get; set; }

        [Required]
        public string Password { get; set; }
    }
}

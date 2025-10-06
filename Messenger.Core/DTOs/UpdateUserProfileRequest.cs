using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs
{
    public class UpdateUserProfileRequest
    {
        [MaxLength(50)]
        [Required]
        public string LastName { get; set; }

        [MaxLength(50)]
        [Required]
        public string FirstName { get; set; }

        [MaxLength(50)]
        public string? MiddleName { get; set; }

        [MaxLength(50)]
        [EmailAddress]
        [Required]
        public string Login { get; set; }

        [MaxLength(18)]
        [Required]
        public string Phone { get; set; }

        public string? Theme { get; set; }
    }
}

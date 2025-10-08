﻿using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.Users
{
    public class UpdateUserProfileRequest
    {
        [MaxLength(50)]
        [Required]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(50)]
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MiddleName { get; set; } = string.Empty;

        [MaxLength(50)]
        [EmailAddress]
        [Required]
        public string Login { get; set; } = string.Empty;

        [MaxLength(18)]
        [Required]
        public string Phone { get; set; } = string.Empty;

        public string? Theme { get; set; } = string.Empty;
    }
}

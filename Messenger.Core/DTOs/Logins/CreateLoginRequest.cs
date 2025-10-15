﻿using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.Logins
{
    public class CreateLoginRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string IpAddress { get; set; } = string.Empty;
    }
}

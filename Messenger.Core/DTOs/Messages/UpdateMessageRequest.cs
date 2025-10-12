﻿using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.Messages
{
    public class UpdateMessageRequest
    {
        [Required]
        public Guid ChatId { get; set; }

        [Required]
        public string MessageText { get; set; } = string.Empty;

        [Required]
        public bool HasAttachmets { get; set; }
    }
}

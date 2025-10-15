using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.DTOs.UserStatuses
{
    public class UpdateStatusRequest
    {
        [Required]
        public bool Online { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Настройки аккаунта"
    /// </summary>
    public class AccountSettings
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string? Avatar { get; set; }
        public string? Theme { get; set; }
        public User? User { get; set; }
    }
}
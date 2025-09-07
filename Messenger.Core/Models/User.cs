using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Пользователи"
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }
        public Guid AccountSettingsId { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Login { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public AccountSettings? AccountSettings { get; set; }
        public List<UserStatus> Status { get; set; } = new List<UserStatus>();
        public List<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
        public List<Message> SentMessages { get; set; } = new List<Message>();
        public List<Message> ReceivedMessages { get; set; } = new List<Message>();
        public List<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public List<LoginSession> Logins { get; set; } = new List<LoginSession>();
        public List<BlackList> BlockedUsers { get; set; } = new List<BlackList>();
        public List<BlackList> BlockedByUsers { get; set; } = new List<BlackList>();
    }
}
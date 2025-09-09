using System.ComponentModel.DataAnnotations;

namespace Messenger.Core.Models
{
    /// <summary>
    /// Класс модели сущности "Пользователи"
    /// </summary>
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AccountSettingsId { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MiddleName { get; set; }

        [Required]
        public DateTime BirthDate { get; set; }

        [Required]
        public DateTime RegistrationDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Login { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(18)]
        public string Phone { get; set; } = string.Empty;

        public AccountSettings? AccountSettings { get; set; }
        public List<UserStatus> UserStatuses { get; set; } = new List<UserStatus>();
        public List<Chat> Chats { get; set; } = new List<Chat>();
        public List<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
        public List<Message> SentMessages { get; set; } = new List<Message>();
        public List<Message> ReceivedMessages { get; set; } = new List<Message>();
        public List<MessageStatus> MessageStatuses { get; set; } = new List<MessageStatus>();
        public List<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public List<LoginSession> Logins { get; set; } = new List<LoginSession>();
        public List<BlackList> BlockedUsers { get; set; } = new List<BlackList>();
        public List<BlackList> BlockedByUsers { get; set; } = new List<BlackList>();
    }
}
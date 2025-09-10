using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Messenger.Core.Models;

public partial class User
{
    [Key]
    [Column("User_ID")]
    public Guid UserId { get; set; }

    [Column("Account_ID")]
    public Guid AccountId { get; set; }

    [Column("Last_Name")]
    [StringLength(50)]
    public string LastName { get; set; } = null!;

    [Column("First_Name")]
    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [Column("Middle_Name")]
    [StringLength(50)]
    public string? MiddleName { get; set; }

    [Column("Birth_Date")]
    public DateOnly BirthDate { get; set; }

    [Column("Registration_Date")]
    public DateOnly RegistrationDate { get; set; }

    [StringLength(20)]
    public string Login { get; set; } = null!;

    [StringLength(255)]
    public string Password { get; set; } = null!;

    [StringLength(18)]
    public string Phone { get; set; } = null!;

    [ForeignKey("AccountId")]
    [InverseProperty("Users")]
    public virtual AccountSetting Account { get; set; } = null!;

    [InverseProperty("BlockedUser")]
    public virtual ICollection<Blacklist> BlacklistBlockedUsers { get; set; } = new List<Blacklist>();

    [InverseProperty("User")]
    public virtual ICollection<Blacklist> BlacklistUsers { get; set; } = new List<Blacklist>();

    [InverseProperty("User")]
    public virtual ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();

    [InverseProperty("User")]
    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    [InverseProperty("User")]
    public virtual ICollection<Login> Logins { get; set; } = new List<Login>();

    [InverseProperty("Recipient")]
    public virtual ICollection<Message> MessageRecipients { get; set; } = new List<Message>();

    [InverseProperty("Sender")]
    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    [InverseProperty("User")]
    public virtual ICollection<MessageStatus> MessageStatuses { get; set; } = new List<MessageStatus>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

    [InverseProperty("User")]
    public virtual UserStatus? UserStatus { get; set; }
}

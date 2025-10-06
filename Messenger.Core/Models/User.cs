using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

    [StringLength(50)]
    public string Login { get; set; } = null!;

    [StringLength(255)]
    public string Password { get; set; } = null!;

    [StringLength(18)]
    public string Phone { get; set; } = null!;

    [ForeignKey("AccountId")]
    [InverseProperty("Users")]
    [JsonIgnore]
    public virtual AccountSetting Account { get; set; } = null!;

    [InverseProperty("BlockedUser")]
    [JsonIgnore]
    public virtual ICollection<Blacklist> BlacklistBlockedUsers { get; set; } = new List<Blacklist>();

    [InverseProperty("User")]
    [JsonIgnore]
    public virtual ICollection<Blacklist> BlacklistUsers { get; set; } = new List<Blacklist>();

    [InverseProperty("User")]
    [JsonIgnore]
    public virtual ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();

    [InverseProperty("User")]
    [JsonIgnore]
    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    [InverseProperty("User")]
    [JsonIgnore]
    public virtual ICollection<Login> Logins { get; set; } = new List<Login>();

    [InverseProperty("Recipient")]
    [JsonIgnore]
    public virtual ICollection<Message> MessageRecipients { get; set; } = new List<Message>();

    [InverseProperty("Sender")]
    [JsonIgnore]
    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    [InverseProperty("User")]
    [JsonIgnore]
    public virtual ICollection<MessageStatus> MessageStatuses { get; set; } = new List<MessageStatus>();

    [InverseProperty("User")]
    [JsonIgnore]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    [JsonIgnore]
    public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

    [InverseProperty("User")]
    [JsonIgnore]
    public virtual UserStatus? UserStatus { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Users")]
    [JsonIgnore]
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}

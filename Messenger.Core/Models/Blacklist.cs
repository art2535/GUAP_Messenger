using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models;

[PrimaryKey("UserId", "BlockedUserId")]
[Table("Blacklist")]
public partial class Blacklist
{
    [Key]
    [Column("User_ID")]
    public Guid UserId { get; set; }

    [Key]
    [Column("Blocked_User_ID")]
    public Guid BlockedUserId { get; set; }

    [Column("Block_Date", TypeName = "timestamp without time zone")]
    public DateTime? BlockDate { get; set; }

    [ForeignKey("BlockedUserId")]
    [InverseProperty("BlacklistBlockedUsers")]
    [JsonIgnore]
    public virtual User BlockedUser { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("BlacklistUsers")]
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}

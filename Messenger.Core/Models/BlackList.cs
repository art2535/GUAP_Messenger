using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

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
    public virtual User BlockedUser { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("BlacklistUsers")]
    public virtual User User { get; set; } = null!;
}
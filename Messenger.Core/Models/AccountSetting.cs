using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Core.Models;

[Table("Account_Settings")]
[Index("AccountId", Name = "Account_Settings_Account_ID_key", IsUnique = true)]
public partial class AccountSetting
{
    [Key]
    [Column("Setting_ID")]
    public Guid SettingId { get; set; }

    [Column("Account_ID")]
    public Guid AccountId { get; set; }

    public string? Avatar { get; set; }

    [StringLength(15)]
    public string? Theme { get; set; }

    [InverseProperty("Account")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}

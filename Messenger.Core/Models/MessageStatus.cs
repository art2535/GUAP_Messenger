using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models;

[PrimaryKey("MessageId", "UserId")]
[Table("Message_Statuses")]
public partial class MessageStatus
{
    [Key]
    [Column("Message_ID")]
    public Guid MessageId { get; set; }

    [Key]
    [Column("User_ID")]
    public Guid UserId { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column("Change_Date", TypeName = "timestamp without time zone")]
    public DateTime ChangeDate { get; set; }

    [ForeignKey("MessageId")]
    [InverseProperty("MessageStatuses")]
    [JsonIgnore]
    public virtual Message Message { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("MessageStatuses")]
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}

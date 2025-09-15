using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Messenger.Core.Models;

public partial class Reaction
{
    [Key]
    [Column("Reaction_ID")]
    public Guid ReactionId { get; set; }

    [Column("Message_ID")]
    public Guid MessageId { get; set; }

    [Column("User_ID")]
    public Guid UserId { get; set; }

    [Column("Reaction_Type")]
    [StringLength(30)]
    public string ReactionType { get; set; } = null!;

    [ForeignKey("MessageId")]
    [InverseProperty("Reactions")]
    public virtual Message Message { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Reactions")]
    public virtual User User { get; set; } = null!;
}

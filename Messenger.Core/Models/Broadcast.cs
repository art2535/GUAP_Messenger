using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models;

[Table("Broadcasts")]
public partial class Broadcast
{
    [Key]
    [Column("Broadcast_ID")]
    public Guid BroadcastId { get; set; }

    [Required]
    [StringLength(200)]
    [Column("Title")]
    public string Title { get; set; } = null!;

    [Required]
    [Column("Message_Text")]
    public string MessageText { get; set; } = null!;

    [Required]
    [Column("Sender_ID")]
    public Guid SenderId { get; set; }

    [Column("Created_At")]
    public DateTime CreatedAt { get; set; }

    [Column("Total_Recipients")]
    public int TotalRecipients { get; set; }

    [ForeignKey(nameof(SenderId))]
    [InverseProperty(nameof(User.BroadcastsCreated))]
    [JsonIgnore]
    public virtual User Sender { get; set; } = null!;

    [InverseProperty(nameof(BroadcastRecipient.Broadcast))]
    [JsonIgnore]
    public virtual ICollection<BroadcastRecipient> Recipients { get; set; } = new List<BroadcastRecipient>();
}
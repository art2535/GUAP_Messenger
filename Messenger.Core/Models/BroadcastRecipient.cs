using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models;

[Table("Broadcast_Recipients")]
public partial class BroadcastRecipient
{
    [Key]
    [Column("Broadcast_ID")]
    public Guid BroadcastId { get; set; }

    [Key]
    [Column("User_ID")]
    public Guid UserId { get; set; }

    [Column("Sent_At")]
    public DateTime SentAt { get; set; }

    [Column("Delivered_At")]
    public DateTime? DeliveredAt { get; set; }

    [Column("Read_At")]
    public DateTime? ReadAt { get; set; }

    [Column("Is_Read")]
    public bool IsRead { get; set; }

    // Навигация
    [ForeignKey(nameof(BroadcastId))]
    [InverseProperty(nameof(Broadcast.Recipients))]
    [JsonIgnore]
    public virtual Broadcast Broadcast { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.BroadcastRecipients))]
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}
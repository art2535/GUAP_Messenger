﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Core.Models;

[PrimaryKey("ChatId", "UserId")]
[Table("Chat_Participants")]
public partial class ChatParticipant
{
    [Key]
    [Column("Chat_ID")]
    public Guid ChatId { get; set; }

    [Key]
    [Column("User_ID")]
    public Guid UserId { get; set; }

    [StringLength(20)]
    public string? Role { get; set; }

    [Column("Join_Date", TypeName = "timestamp without time zone")]
    public DateTime JoinDate { get; set; }

    [ForeignKey("ChatId")]
    [InverseProperty("ChatParticipants")]
    public virtual Chat Chat { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("ChatParticipants")]
    public virtual User User { get; set; } = null!;
}
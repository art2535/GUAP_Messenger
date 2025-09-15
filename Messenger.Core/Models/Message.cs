using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Messenger.Core.Models;

public partial class Message
{
    [Key]
    [Column("Message_ID")]
    public Guid MessageId { get; set; }

    [Column("Sender_ID")]
    public Guid SenderId { get; set; }

    [Column("Recipient_ID")]
    public Guid RecipientId { get; set; }

    [Column("Chat_ID")]
    public Guid ChatId { get; set; }

    [Column("Message_Text")]
    public string MessageText { get; set; } = null!;

    [Column("Has_Attachments")]
    public bool HasAttachments { get; set; }

    [Column("Send_Time", TypeName = "timestamp without time zone")]
    public DateTime SendTime { get; set; }

    [InverseProperty("Message")]
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    [ForeignKey("ChatId")]
    [InverseProperty("Messages")]
    public virtual Chat Chat { get; set; } = null!;

    [InverseProperty("Message")]
    public virtual ICollection<MessageStatus> MessageStatuses { get; set; } = new List<MessageStatus>();

    [InverseProperty("Message")]
    public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

    [ForeignKey("RecipientId")]
    [InverseProperty("MessageRecipients")]
    public virtual User Recipient { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("MessageSenders")]
    public virtual User Sender { get; set; } = null!;
}

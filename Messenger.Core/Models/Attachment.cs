using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Messenger.Core.Models;

public partial class Attachment
{
    [Key]
    [Column("Attachment_ID")]
    public Guid AttachmentId { get; set; }

    [Column("Message_ID")]
    public Guid MessageId { get; set; }

    [Column("File_Name")]
    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [Column("File_Type")]
    [StringLength(100)]
    public string? FileType { get; set; }

    [Column("Size_in_Bytes")]
    public int? SizeInBytes { get; set; }

    [Column("URL")]
    public string Url { get; set; } = null!;

    [ForeignKey("MessageId")]
    [InverseProperty("Attachments")]
    public virtual Message Message { get; set; } = null!;
}
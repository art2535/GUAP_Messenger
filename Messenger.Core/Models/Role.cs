using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Messenger.Core.Models;

public partial class Role
{
    [Key]
    [Column("Role_ID")]
    public Guid RoleId { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("Roles")]
    [JsonIgnore]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}

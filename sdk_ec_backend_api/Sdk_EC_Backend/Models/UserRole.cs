using Postgrest.Attributes;
using Postgrest.Models;

namespace Sdk_EC_Backend.Models;

[Table("user_roles")]
public class UserRole : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("role")]
    public string Role { get; set; } = "customer";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

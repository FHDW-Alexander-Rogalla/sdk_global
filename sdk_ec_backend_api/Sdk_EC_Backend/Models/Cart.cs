using Postgrest.Attributes;
using Postgrest.Models;

namespace Sdk_EC_Backend.Models;

[Table("carts")]
public class Cart : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

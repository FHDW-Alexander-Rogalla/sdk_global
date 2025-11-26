using Postgrest.Attributes;
using Postgrest.Models;

namespace Sdk_EC_Backend.Models;

[Table("orders")]
public class Order : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

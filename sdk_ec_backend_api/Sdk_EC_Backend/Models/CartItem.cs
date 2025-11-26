using Postgrest.Attributes;
using Postgrest.Models;

namespace Sdk_EC_Backend.Models;

[Table("cart_items")]
public class CartItem : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("cart_id")]
    public long CartId { get; set; }

    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }
}

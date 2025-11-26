using Postgrest.Attributes;
using Postgrest.Models;

namespace Sdk_EC_Backend.Models;

[Table("order_items")]
public class OrderItem : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("order_id")]
    public long OrderId { get; set; }

    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price_at_purchase")]
    public decimal PriceAtPurchase { get; set; }
}

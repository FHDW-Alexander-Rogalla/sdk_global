using Postgrest.Attributes;
using Postgrest.Models;

namespace Sdk_EC_Backend.Models;

[Table("products")]
public class Product : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // public int StockQuantity { get; set; }
    // public string? Category { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
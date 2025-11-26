namespace Sdk_EC_Backend.Models.Dtos;

public class CartItemDto
{
    public long Id { get; set; }
    public long CartId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}

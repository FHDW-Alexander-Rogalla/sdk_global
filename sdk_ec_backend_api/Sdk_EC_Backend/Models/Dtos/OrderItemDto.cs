namespace Sdk_EC_Backend.Models.Dtos;

public class OrderItemDto
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
}

namespace Sdk_EC_Backend.Models.Dtos;

public class OrderDto
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

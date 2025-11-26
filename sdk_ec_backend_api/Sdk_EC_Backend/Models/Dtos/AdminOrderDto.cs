namespace Sdk_EC_Backend.Models.Dtos;

/// <summary>
/// Extended OrderDto for Admin views with additional user information
/// </summary>
public class AdminOrderDto
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? Username { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

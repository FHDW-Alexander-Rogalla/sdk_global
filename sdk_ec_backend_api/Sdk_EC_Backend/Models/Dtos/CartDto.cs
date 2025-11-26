namespace Sdk_EC_Backend.Models.Dtos;

public class CartDto
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

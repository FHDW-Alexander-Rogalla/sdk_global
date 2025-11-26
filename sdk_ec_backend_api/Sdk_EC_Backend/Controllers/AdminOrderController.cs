using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sdk_EC_Backend.Models;
using Sdk_EC_Backend.Models.Dtos;
using Sdk_EC_Backend.Services;
using System.Security.Claims;

namespace Sdk_EC_Backend.Controllers;

[ApiController]
[Route("api/admin/order")]
[Authorize] // Requires JWT authentication
public class AdminOrderController : ControllerBase
{
    private readonly SupabaseService _supabaseService;

    public AdminOrderController(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// Gets the current user's ID from the JWT token
    /// </summary>
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        return userId;
    }

    /// <summary>
    /// Checks if the current user is an admin by querying the user_roles table
    /// </summary>
    private async Task<bool> IsAdmin()
    {
        try
        {
            var userId = GetUserId();

            var roleResponse = await _supabaseService.Client
                .From<UserRole>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            if (roleResponse.Models.Count == 0)
            {
                return false;
            }

            var userRole = roleResponse.Models.First();
            return userRole.Role.Equals("admin", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// GET /api/admin/order - Gets all orders from all users (Admin only)
    /// Returns orders with full details including user info, order items, and product details
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminOrderDto>>> GetAllOrders()
    {
        try
        {
            // Check if user is admin
            if (!await IsAdmin())
            {
                return Forbid();
            }

            // Get all orders (no user filter)
            var ordersResponse = await _supabaseService.Client
                .From<Order>()
                .Order("order_date", Postgrest.Constants.Ordering.Descending)
                .Get();

            var orders = ordersResponse.Models;

            // Build detailed order DTOs with items, product info, and user info
            var adminOrderDtos = new List<AdminOrderDto>();
            
            foreach (var order in orders)
            {
                // Get order items
                var itemsResponse = await _supabaseService.Client
                    .From<OrderItem>()
                    .Filter("order_id", Postgrest.Constants.Operator.Equals, order.Id.ToString())
                    .Get();

                var items = itemsResponse.Models.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    PriceAtPurchase = oi.PriceAtPurchase
                }).ToList();

                // Calculate total amount
                var totalAmount = items.Sum(item => item.Quantity * item.PriceAtPurchase);

                var adminOrderDto = new AdminOrderDto
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    UserEmail = null, // Can be populated later if needed
                    Username = null, // Can be populated later if needed
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    UpdatedAt = order.UpdatedAt,
                    Items = items,
                    TotalAmount = totalAmount
                };

                adminOrderDtos.Add(adminOrderDto);
            }

            return Ok(adminOrderDtos);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in GetAllOrders: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return Problem(title: "Failed to fetch all orders", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// GET /api/admin/order/{id} - Gets a specific order by ID (Admin only)
    /// Returns full order details with user information regardless of which user it belongs to
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<AdminOrderDto>> GetOrderById(long id)
    {
        try
        {
            // Check if user is admin
            if (!await IsAdmin())
            {
                return Forbid(); // 403 Forbidden
            }

            // Get order (no user filter)
            var orderResponse = await _supabaseService.Client
                .From<Order>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            if (orderResponse.Models.Count == 0)
            {
                return NotFound(new { message = "Order not found" });
            }

            var order = orderResponse.Models.First();

            // Get order items
            var itemsResponse = await _supabaseService.Client
                .From<OrderItem>()
                .Filter("order_id", Postgrest.Constants.Operator.Equals, order.Id.ToString())
                .Get();

            var items = itemsResponse.Models.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                OrderId = oi.OrderId,
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                PriceAtPurchase = oi.PriceAtPurchase
            }).ToList();

            // Calculate total amount
            var totalAmount = items.Sum(item => item.Quantity * item.PriceAtPurchase);

            var adminOrderDto = new AdminOrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserEmail = null, // Can be populated later if needed
                Username = null, // Can be populated later if needed
                OrderDate = order.OrderDate,
                Status = order.Status,
                UpdatedAt = order.UpdatedAt,
                Items = items,
                TotalAmount = totalAmount
            };

            return Ok(adminOrderDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(title: "Failed to fetch order", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// PATCH /api/admin/order/{id}/status - Updates the status of any order (Admin only)
    /// Valid statuses: pending, confirmed, payment pending, payment received, delivered, canceled
    /// </summary>
    [HttpPatch("{id:long}/status")]
    public async Task<ActionResult<AdminOrderDto>> UpdateOrderStatus(long id, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            // Check if user is admin
            if (!await IsAdmin())
            {
                return Forbid(); // 403 Forbidden
            }

            // Validate status (case-insensitive)
            var validStatuses = new[] { "pending", "confirmed", "payment_pending", "payment_received", "delivered", "canceled" };
            if (!validStatuses.Contains(request.Status.ToLower()))
            {
                return BadRequest(new { message = $"Invalid status. Valid statuses are: {string.Join(", ", validStatuses)}" });
            }

            // Get order (no user filter needed for admin)
            var orderResponse = await _supabaseService.Client
                .From<Order>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            if (orderResponse.Models.Count == 0)
            {
                return NotFound(new { message = "Order not found" });
            }

            var order = orderResponse.Models.First();
            order.Status = request.Status;

            var updateResponse = await _supabaseService.Client
                .From<Order>()
                .Update(order);

            var updatedOrder = updateResponse.Models.First();

            // Get order items for response
            var itemsResponse = await _supabaseService.Client
                .From<OrderItem>()
                .Filter("order_id", Postgrest.Constants.Operator.Equals, updatedOrder.Id.ToString())
                .Get();

            var items = itemsResponse.Models.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                OrderId = oi.OrderId,
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                PriceAtPurchase = oi.PriceAtPurchase
            }).ToList();

            // Calculate total
            var totalAmount = items.Sum(item => item.Quantity * item.PriceAtPurchase);

            var adminOrderDto = new AdminOrderDto
            {
                Id = updatedOrder.Id,
                UserId = updatedOrder.UserId,
                UserEmail = null, // Can be populated later if needed
                Username = null, // Can be populated later if needed
                OrderDate = updatedOrder.OrderDate,
                Status = updatedOrder.Status,
                UpdatedAt = updatedOrder.UpdatedAt,
                Items = items,
                TotalAmount = totalAmount
            };

            return Ok(adminOrderDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in UpdateOrderStatus: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return Problem(title: "Failed to update order status", detail: ex.Message, statusCode: 500);
        }
    }
}

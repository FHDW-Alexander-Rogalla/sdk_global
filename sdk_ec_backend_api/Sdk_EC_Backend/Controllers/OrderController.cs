using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sdk_EC_Backend.Models;
using Sdk_EC_Backend.Models.Dtos;
using Sdk_EC_Backend.Services;
using System.Security.Claims;

namespace Sdk_EC_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires JWT authentication
public class OrderController : ControllerBase
{
    private readonly SupabaseService _supabaseService;

    public OrderController(SupabaseService supabaseService)
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
    /// POST /api/order/checkout - Creates an order from the user's current cart
    /// Converts all cart items to order items and empties the cart
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> CheckoutCart()
    {
        try
        {
            var userId = GetUserId();

            // 1. Get user's cart
            var cartResponse = await _supabaseService.Client
                .From<Cart>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            if (cartResponse.Models.Count == 0)
            {
                return BadRequest(new { message = "No cart found for user" });
            }

            var cart = cartResponse.Models.First();

            // 2. Get all cart items
            var cartItemsResponse = await _supabaseService.Client
                .From<CartItem>()
                .Filter("cart_id", Postgrest.Constants.Operator.Equals, cart.Id.ToString())
                .Get();

            if (cartItemsResponse.Models.Count == 0)
            {
                return BadRequest(new { message = "Cart is empty" });
            }

            var cartItems = cartItemsResponse.Models;

            // 3. Get product prices for all items in cart (only active products)
            var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
            var productsResponse = await _supabaseService.Client
                .From<Product>()
                .Filter("id", Postgrest.Constants.Operator.In, productIds)
                .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
                .Get();

            var products = productsResponse.Models.ToDictionary(p => p.Id, p => p);

            // Check if all cart items have valid active products
            var inactiveProductIds = cartItems
                .Where(ci => !products.ContainsKey(ci.ProductId))
                .Select(ci => ci.ProductId)
                .ToList();

            if (inactiveProductIds.Any())
            {
                return BadRequest(new 
                { 
                    message = "Some products in your cart are no longer available",
                    inactiveProductIds = inactiveProductIds
                });
            }

            // 4. Create new order
            var now = DateTime.UtcNow;
            var newOrder = new Order
            {
                UserId = userId,
                OrderDate = now,
                Status = "pending",
                UpdatedAt = now
            };

            var orderResponse = await _supabaseService.Client
                .From<Order>()
                .Insert(newOrder);

            var order = orderResponse.Models.First();

            // 5. Create order items from cart items
            var orderItems = new List<OrderItem>();
            foreach (var cartItem in cartItems)
            {
                if (!products.TryGetValue(cartItem.ProductId, out var product))
                {
                    // Skip if product not found (should not happen normally)
                    continue;
                }

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    PriceAtPurchase = product.Price
                };

                orderItems.Add(orderItem);
            }

            // Insert all order items
            await _supabaseService.Client
                .From<OrderItem>()
                .Insert(orderItems);

            // 6. Delete all cart items
            foreach (var cartItem in cartItems)
            {
                await _supabaseService.Client
                    .From<CartItem>()
                    .Where(x => x.Id == cartItem.Id)
                    .Delete();
            }

            // 7. Return created order with items
            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                UpdatedAt = order.UpdatedAt,
                Items = orderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    PriceAtPurchase = oi.PriceAtPurchase
                }).ToList()
            };

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, orderDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in CheckoutCart: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return Problem(title: "Failed to checkout cart", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// GET /api/order - Gets all orders for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetUserOrders()
    {
        try
        {
            var userId = GetUserId();

            // Get all orders for user
            var ordersResponse = await _supabaseService.Client
                .From<Order>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Order("order_date", Postgrest.Constants.Ordering.Descending)
                .Get();

            var orders = ordersResponse.Models;

            // Get all order items for these orders
            var orderDtos = new List<OrderDto>();
            foreach (var order in orders)
            {
                var itemsResponse = await _supabaseService.Client
                    .From<OrderItem>()
                    .Filter("order_id", Postgrest.Constants.Operator.Equals, order.Id.ToString())
                    .Get();

                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    UpdatedAt = order.UpdatedAt,
                    Items = itemsResponse.Models.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        OrderId = oi.OrderId,
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        PriceAtPurchase = oi.PriceAtPurchase
                    }).ToList()
                };

                orderDtos.Add(orderDto);
            }

            return Ok(orderDtos);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(title: "Failed to fetch orders", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// GET /api/order/{id} - Gets a specific order by ID (only if it belongs to the user)
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(long id)
    {
        try
        {
            var userId = GetUserId();

            // Get order and verify it belongs to user
            var orderResponse = await _supabaseService.Client
                .From<Order>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
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

            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                UpdatedAt = order.UpdatedAt,
                Items = itemsResponse.Models.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    PriceAtPurchase = oi.PriceAtPurchase
                }).ToList()
            };

            return Ok(orderDto);
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
    /// PATCH /api/order/{id}/cancel - Cancels an order (only if not delivered)
    /// Users can only cancel their own orders and only if status is not 'delivered'
    /// </summary>
    [HttpPatch("{id:long}/cancel")]
    public async Task<ActionResult<OrderDto>> CancelOrder(long id)
    {
        try
        {
            var userId = GetUserId();

            // Get order and verify it belongs to user
            var orderResponse = await _supabaseService.Client
                .From<Order>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            if (orderResponse.Models.Count == 0)
            {
                return NotFound(new { message = "Order not found" });
            }

            var order = orderResponse.Models.First();

            // Check if order can be cancelled
            if (order.Status.Equals("delivered", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Cannot cancel a delivered order" });
            }

            if (order.Status.Equals("canceled", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Order is already canceled" });
            }

            // Update status to canceled
            order.Status = "canceled";
            order.UpdatedAt = DateTime.UtcNow;

            var updateResponse = await _supabaseService.Client
                .From<Order>()
                .Update(order);

            var updatedOrder = updateResponse.Models.First();

            // Get order items
            var itemsResponse = await _supabaseService.Client
                .From<OrderItem>()
                .Filter("order_id", Postgrest.Constants.Operator.Equals, updatedOrder.Id.ToString())
                .Get();

            var orderDto = new OrderDto
            {
                Id = updatedOrder.Id,
                UserId = updatedOrder.UserId,
                OrderDate = updatedOrder.OrderDate,
                Status = updatedOrder.Status,
                UpdatedAt = updatedOrder.UpdatedAt,
                Items = itemsResponse.Models.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    PriceAtPurchase = oi.PriceAtPurchase
                }).ToList()
            };

            return Ok(orderDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in CancelOrder: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return Problem(title: "Failed to cancel order", detail: ex.Message, statusCode: 500);
        }
    }

    // /// <summary>
    // /// PATCH /api/order/{id}/status - Updates the status of an order
    // /// Deprecated: Users should use /cancel endpoint instead
    // /// </summary>
    // [HttpPatch("{id:long}/status")]
    // public async Task<ActionResult<OrderDto>> UpdateOrderStatus(long id, [FromBody] UpdateOrderStatusRequest request)
    // {
    //     try
    //     {
    //         var userId = GetUserId();

    //         // Get order and verify it belongs to user
    //         var orderResponse = await _supabaseService.Client
    //             .From<Order>()
    //             .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
    //             .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
    //             .Get();

    //         if (orderResponse.Models.Count == 0)
    //         {
    //             return NotFound(new { message = "Order not found" });
    //         }

    //         var order = orderResponse.Models.First();
    //         order.Status = request.Status;
    //         order.UpdatedAt = DateTime.UtcNow;

    //         var updateResponse = await _supabaseService.Client
    //             .From<Order>()
    //             .Update(order);

    //         var updatedOrder = updateResponse.Models.First();

    //         // Get order items
    //         var itemsResponse = await _supabaseService.Client
    //             .From<OrderItem>()
    //             .Filter("order_id", Postgrest.Constants.Operator.Equals, updatedOrder.Id.ToString())
    //             .Get();

    //         var orderDto = new OrderDto
    //         {
    //             Id = updatedOrder.Id,
    //             UserId = updatedOrder.UserId,
    //             OrderDate = updatedOrder.OrderDate,
    //             Status = updatedOrder.Status,
    //             UpdatedAt = updatedOrder.UpdatedAt,
    //             Items = itemsResponse.Models.Select(oi => new OrderItemDto
    //             {
    //                 Id = oi.Id,
    //                 OrderId = oi.OrderId,
    //                 ProductId = oi.ProductId,
    //                 Quantity = oi.Quantity,
    //                 PriceAtPurchase = oi.PriceAtPurchase
    //             }).ToList()
    //         };

    //         return Ok(orderDto);
    //     }
    //     catch (UnauthorizedAccessException ex)
    //     {
    //         return Unauthorized(new { message = ex.Message });
    //     }
    //     catch (Exception ex)
    //     {
    //         return Problem(title: "Failed to update order status", detail: ex.Message, statusCode: 500);
    //     }
    // }
}

// Request DTO
public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
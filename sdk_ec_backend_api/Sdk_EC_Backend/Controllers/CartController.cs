using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sdk_EC_Backend.Models;
using Sdk_EC_Backend.Models.Dtos;
using Sdk_EC_Backend.Services;
using Postgrest;
using System.Security.Claims;

namespace Sdk_EC_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires JWT authentication
public class CartController : ControllerBase
{
    private readonly SupabaseService _supabaseService;

    public CartController(SupabaseService supabaseService)
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
    /// GET /api/cart - Gets the current user's cart with all items
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        try
        {
            Console.WriteLine("=== GetCart called ===");
            Console.WriteLine($"User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            // RLS forwarding removed for cleaner code; backend now runs without forwarding user JWT to PostgREST.
            
            var userId = GetUserId();
            Console.WriteLine($"User ID: {userId}");

            // Get or create cart for user
            var cartResponse = await _supabaseService.Client
                .From<Cart>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            Cart cart;
            if (cartResponse.Models.Count == 0)
            {
                // Create new cart for user
                var newCart = new Cart { UserId = userId };
                var insertResponse = await _supabaseService.Client
                    .From<Cart>()
                    .Insert(newCart);
                cart = insertResponse.Models.First();
            }
            else
            {
                cart = cartResponse.Models.First();
            }

            var cartDto = new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt
            };

            return Ok(cartDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Unauthorized: {ex.Message}");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in GetCart: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return Problem(title: "Failed to fetch cart", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// GET /api/cart/items - Gets all items in the current user's cart
    /// </summary>
    [HttpGet("items")]
    public async Task<ActionResult<IEnumerable<CartItemDto>>> GetCartItems()
    {
        try
        {
            // RLS forwarding removed for cleaner code.

            var userId = GetUserId();

            // Get user's cart
            var cartResponse = await _supabaseService.Client
                .From<Cart>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            if (cartResponse.Models.Count == 0)
            {
                return Ok(new List<CartItemDto>()); // Empty cart
            }

            var cart = cartResponse.Models.First();

            // Get cart items
            var itemsResponse = await _supabaseService.Client
                .From<CartItem>()
                .Filter("cart_id", Postgrest.Constants.Operator.Equals, cart.Id.ToString())
                .Get();

            var dtos = itemsResponse.Models.Select(item => new CartItemDto
            {
                Id = item.Id,
                CartId = item.CartId,
                ProductId = item.ProductId,
                Quantity = item.Quantity
            });

            return Ok(dtos);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(title: "Failed to fetch cart items", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// POST /api/cart/items - Adds an item to the cart or updates quantity if exists
    /// </summary>
    [HttpPost("items")]
    public async Task<ActionResult<CartItemDto>> AddCartItem([FromBody] AddCartItemRequest request)
    {
        try
        {
            // RLS forwarding removed for cleaner code.

            var userId = GetUserId();

            // Get or create cart
            var cartResponse = await _supabaseService.Client
                .From<Cart>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            Cart cart;
            if (cartResponse.Models.Count == 0)
            {
                var newCart = new Cart { UserId = userId };
                var insertResponse = await _supabaseService.Client
                    .From<Cart>()
                    .Insert(newCart);
                cart = insertResponse.Models.First();
            }
            else
            {
                cart = cartResponse.Models.First();
            }

            // Check if product exists and is active
            var productResponse = await _supabaseService.Client
                .From<Product>()
                .Filter("id", Postgrest.Constants.Operator.Equals, request.ProductId.ToString())
                .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
                .Get();

            if (productResponse.Models.Count == 0)
            {
                return BadRequest(new { message = "Product is not available or has been deactivated" });
            }

            // Check if item already exists in cart
            var existingItemResponse = await _supabaseService.Client
                .From<CartItem>()
                .Filter("cart_id", Postgrest.Constants.Operator.Equals, cart.Id.ToString())
                .Filter("product_id", Postgrest.Constants.Operator.Equals, request.ProductId.ToString())
                .Get();

            CartItem cartItem;
            if (existingItemResponse.Models.Count > 0)
            {
                // Update existing item quantity
                cartItem = existingItemResponse.Models.First();
                cartItem.Quantity += request.Quantity;
                
                var updateResponse = await _supabaseService.Client
                    .From<CartItem>()
                    .Update(cartItem);
                cartItem = updateResponse.Models.First();
            }
            else
            {
                // Insert new item
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                };
                
                var insertResponse = await _supabaseService.Client
                    .From<CartItem>()
                    .Insert(cartItem);
                cartItem = insertResponse.Models.First();
            }

            var dto = new CartItemDto
            {
                Id = cartItem.Id,
                CartId = cartItem.CartId,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity
            };

            return CreatedAtAction(nameof(GetCartItems), new { id = dto.Id }, dto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(title: "Failed to add cart item", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// PUT /api/cart/items/{id} - Updates the quantity of a cart item
    /// </summary>
    [HttpPut("items/{id:long}")]
    public async Task<ActionResult<CartItemDto>> UpdateCartItem(long id, [FromBody] UpdateCartItemRequest request)
    {
        try
        {
            // RLS forwarding removed for cleaner code.

            var userId = GetUserId();

            // Verify item belongs to user's cart
            var itemResponse = await _supabaseService.Client
                .From<CartItem>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            if (itemResponse.Models.Count == 0)
            {
                return NotFound();
            }

            var cartItem = itemResponse.Models.First();

            // Verify cart belongs to user
            var cartResponse = await _supabaseService.Client
                .From<Cart>()
                .Filter("id", Postgrest.Constants.Operator.Equals, cartItem.CartId.ToString())
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            if (cartResponse.Models.Count == 0)
            {
                return Forbid(); // Item exists but doesn't belong to this user
            }

            // Update quantity
            cartItem.Quantity = request.Quantity;
            var updateResponse = await _supabaseService.Client
                .From<CartItem>()
                .Update(cartItem);

            var updated = updateResponse.Models.First();
            var dto = new CartItemDto
            {
                Id = updated.Id,
                CartId = updated.CartId,
                ProductId = updated.ProductId,
                Quantity = updated.Quantity
            };

            return Ok(dto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(title: "Failed to update cart item", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// DELETE /api/cart/items/{id} - Removes an item from the cart
    /// </summary>
    [HttpDelete("items/{id:long}")]
    public async Task<ActionResult> DeleteCartItem(long id)
    {
        try
        {
            // RLS forwarding removed for cleaner code.

            var userId = GetUserId();

            // Verify item belongs to user's cart
            var itemResponse = await _supabaseService.Client
                .From<CartItem>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            if (itemResponse.Models.Count == 0)
            {
                return NotFound();
            }

            var cartItem = itemResponse.Models.First();

            // Verify cart belongs to user
            var cartResponse = await _supabaseService.Client
                .From<Cart>()
                .Filter("id", Postgrest.Constants.Operator.Equals, cartItem.CartId.ToString())
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            if (cartResponse.Models.Count == 0)
            {
                return Forbid();
            }

            // Delete item
            await _supabaseService.Client
                .From<CartItem>()
                .Where(x => x.Id == id)
                .Delete();

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(title: "Failed to delete cart item", detail: ex.Message, statusCode: 500);
        }
    }
}

// Request DTOs
public class AddCartItemRequest
{
    public long ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}

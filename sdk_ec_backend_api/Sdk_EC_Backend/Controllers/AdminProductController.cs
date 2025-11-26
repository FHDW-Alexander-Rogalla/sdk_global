using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sdk_EC_Backend.Models;
using Sdk_EC_Backend.Models.Dtos;
using Sdk_EC_Backend.Services;
using System.Security.Claims;

namespace Sdk_EC_Backend.Controllers;

[ApiController]
[Route("api/admin/product")]
[Authorize] // Requires JWT authentication
public class AdminProductController : ControllerBase
{
    private readonly SupabaseService _supabaseService;

    public AdminProductController(SupabaseService supabaseService)
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
    /// GET /api/admin/product - Gets all products including inactive ones (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts()
    {
        try
        {
            // Check if user is admin
            if (!await IsAdmin())
            {
                return Forbid();
            }

            var response = await _supabaseService.Client
                .From<Product>()
                .Get();

            var dtos = response.Models.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            });

            return Ok(dtos);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in GetAllProducts: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return Problem(title: "Failed to fetch products", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// POST /api/admin/product - Creates a new product (Admin only)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            // Check if user is admin
            if (!await IsAdmin())
            {
                return Forbid();
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Product name is required" });
            }

            if (request.Price < 0)
            {
                return BadRequest(new { message = "Price must be greater than or equal to 0" });
            }

            // Create new product with current timestamps
            var now = DateTime.UtcNow;
            var newProduct = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                ImageUrl = request.ImageUrl,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var response = await _supabaseService.Client
                .From<Product>()
                .Insert(newProduct);

            var product = response.Models.First();

            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return Created($"/api/product/{dto.Id}", dto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in CreateProduct: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return Problem(title: "Failed to create product", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// PUT /api/admin/product/{id} - Updates an existing product (Admin only)
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(long id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            // Check if user is admin
            if (!await IsAdmin())
            {
                return Forbid();
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Product name is required" });
            }

            if (request.Price < 0)
            {
                return BadRequest(new { message = "Price must be greater than or equal to 0" });
            }

            // Get existing product
            var getResponse = await _supabaseService.Client
                .From<Product>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            if (getResponse.Models.Count == 0)
            {
                return NotFound(new { message = "Product not found" });
            }

            var product = getResponse.Models.First();

            // Update product fields and timestamp
            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.ImageUrl = request.ImageUrl;
            product.UpdatedAt = DateTime.UtcNow;

            var updateResponse = await _supabaseService.Client
                .From<Product>()
                .Update(product);

            var updatedProduct = updateResponse.Models.First();

            var dto = new ProductDto
            {
                Id = updatedProduct.Id,
                Name = updatedProduct.Name,
                Description = updatedProduct.Description,
                Price = updatedProduct.Price,
                ImageUrl = updatedProduct.ImageUrl,
                IsActive = updatedProduct.IsActive,
                CreatedAt = updatedProduct.CreatedAt,
                UpdatedAt = updatedProduct.UpdatedAt
            };

            return Ok(dto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in UpdateProduct: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return Problem(title: "Failed to update product", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// DELETE /api/admin/product/{id} - Soft-deletes a product by setting is_active to false (Admin only)
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ActionResult> DeleteProduct(long id)
    {
        try
        {
            // Check if user is admin
            if (!await IsAdmin())
            {
                return Forbid();
            }

            // Check if product exists
            var getResponse = await _supabaseService.Client
                .From<Product>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            if (getResponse.Models.Count == 0)
            {
                return NotFound(new { message = "Product not found" });
            }

            var product = getResponse.Models.First();

            // Soft-delete: Set is_active to false
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _supabaseService.Client
                .From<Product>()
                .Update(product);

            return Ok(new { message = "Product deactivated successfully", productId = id });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in DeleteProduct: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return Problem(title: "Failed to delete product", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// PATCH /api/admin/product/{id}/activate - Reactivates a product by setting is_active to true (Admin only)
    /// </summary>
    [HttpPatch("{id:long}/activate")]
    public async Task<ActionResult> ActivateProduct(long id)
    {
        try
        {
            // Check if user is admin
            if (!await IsAdmin())
            {
                return Forbid();
            }

            // Check if product exists
            var getResponse = await _supabaseService.Client
                .From<Product>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Get();

            if (getResponse.Models.Count == 0)
            {
                return NotFound(new { message = "Product not found" });
            }

            var product = getResponse.Models.First();

            // Check if already active
            if (product.IsActive)
            {
                return BadRequest(new { message = "Product is already active" });
            }

            // Reactivate: Set is_active to true
            product.IsActive = true;
            product.UpdatedAt = DateTime.UtcNow;

            await _supabaseService.Client
                .From<Product>()
                .Update(product);

            return Ok(new { message = "Product reactivated successfully", productId = id });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in ActivateProduct: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            return Problem(title: "Failed to activate product", detail: ex.Message, statusCode: 500);
        }
    }

}

// Request DTOs
public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
}

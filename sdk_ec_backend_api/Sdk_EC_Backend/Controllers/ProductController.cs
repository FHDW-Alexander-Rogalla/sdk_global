using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sdk_EC_Backend.Models.Dtos;
using Sdk_EC_Backend.Models;
using Sdk_EC_Backend.Services;
using Postgrest;

namespace Sdk_EC_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly SupabaseService _supabaseService;

    public ProductController(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> Get()
    {
        try
        {
            var response = await _supabaseService.Client.From<Product>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Get();
            var dtos = response.Models.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                // StockQuantity = p.StockQuantity,
                // Category = p.Category,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            });
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return Problem(title: "Failed to fetch products", detail: ex.Message, statusCode: 500);
        }
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductDto>> GetById(long id)
    {
        try
        {
            var response = await _supabaseService.Client.From<Product>()
                                       .Filter("id", Constants.Operator.Equals, id.ToString())
                                       .Filter("is_active", Constants.Operator.Equals, "true")
                                       .Get();
            var product = response.Models.FirstOrDefault();
            if (product == null)
                return NotFound();

            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                // StockQuantity = product.StockQuantity,
                // Category = product.Category,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return Problem(title: "Failed to fetch product", detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// GET /api/product/{id}/any - Gets any product by ID (including inactive) for authenticated users
    /// This endpoint is used for cart and order displays where users need to see products they already have
    /// </summary>
    [HttpGet("{id:long}/any")]
    [Authorize]
    public async Task<ActionResult<ProductDto>> GetByIdAny(long id)
    {
        try
        {
            var response = await _supabaseService.Client.From<Product>()
                                       .Filter("id", Constants.Operator.Equals, id.ToString())
                                       .Get();
            var product = response.Models.FirstOrDefault();
            if (product == null)
                return NotFound();

            var dto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                // StockQuantity = product.StockQuantity,
                // Category = product.Category,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return Problem(title: "Failed to fetch product", detail: ex.Message, statusCode: 500);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using tms_template_net8.Models.DTOs;
using tms_template_net8.Models.DTOs.Product;
using tms_template_net8.Services;

namespace tms_template_net8.Controllers.Api;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public IActionResult GetAllProducts()
    {
        var items = _productService.GetAll();
        if (items is null)
            return BadRequest(ApiResponse<IReadOnlyList<ProductItem>>.FailureResponse("Failed to fetch products."));

        return Ok(ApiResponse<IReadOnlyList<ProductItem>>.SuccessResponse(items, "Products fetched successfully."));
    }

    // Preserves the former Web list shape for clients that still call this route.
    [HttpGet("list")]
    public IActionResult GetList()
    {
        var rows = _productService.GetAll()
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Sku,
                x.Price,
                x.Status
            })
            .ToList();

        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public IActionResult GetProductById(int id)
    {
        var item = _productService.GetById(id);
        if (item is null)
            return NotFound(ApiResponse<ProductItem>.FailureResponse("Product not found."));

        return Ok(ApiResponse<ProductItem>.SuccessResponse(item, "Product fetched successfully."));
    }

    [HttpPost]
    public IActionResult CreateProduct([FromBody] ProductUpsertRequest? body)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(ApiResponse<ProductItem>.FailureResponse("Product name is required."));

        var created = _productService.Create(body);
        if (created is null)
            return BadRequest(ApiResponse<ProductItem>.FailureResponse("Failed to create product."));

        return Ok(ApiResponse<ProductItem>.SuccessResponse(created, "Product created successfully."));
    }

    [HttpPut("{id:int}")]
    public IActionResult UpdateProduct(int id, [FromBody] ProductUpsertRequest? body)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(ApiResponse.FailureResponse("Product name is required."));

        var updated = _productService.Update(id, body);
        if (!updated)
            return NotFound(ApiResponse.FailureResponse("Product not found."));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Product updated successfully."));
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteProduct(int id)
    {
        var deleted = _productService.Delete(id);
        if (!deleted)
            return NotFound(ApiResponse.FailureResponse("Product not found."));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Product deleted successfully."));
    }
}

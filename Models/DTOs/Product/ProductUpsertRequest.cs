namespace tms_template_net8.Models.DTOs.Product;

public sealed class ProductUpsertRequest
{
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
}

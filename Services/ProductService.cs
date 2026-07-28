using tms_template_net8.Data.Repositories;
using tms_template_net8.Models.DTOs.Product;

namespace tms_template_net8.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<ProductItem> GetAll()
    {
        return _repository.GetAll();
    }

    public ProductItem? GetById(int id)
    {
        return _repository.GetById(id);
    }

    public ProductItem? Create(ProductUpsertRequest request)
    {
        return _repository.Add(ToProduct(request));
    }

    public bool Update(int id, ProductUpsertRequest request)
    {
        return _repository.Update(id, ToProduct(request));
    }

    public bool Delete(int id)
    {
        return _repository.Delete(id);
    }

    // Map the request to an entity and normalise free-text fields / default the status.
    private static ProductItem ToProduct(ProductUpsertRequest request)
    {
        return new ProductItem
        {
            Name = (request.Name ?? string.Empty).Trim(),
            Sku = (request.Sku ?? string.Empty).Trim(),
            Price = request.Price,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            Description = (request.Description ?? string.Empty).Trim()
        };
    }
}

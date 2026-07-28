using tms_template_net8.Models.DTOs.Product;

namespace tms_template_net8.Services;

public interface IProductService
{
    IReadOnlyList<ProductItem> GetAll();
    ProductItem? GetById(int id);
    ProductItem? Create(ProductUpsertRequest request);
    bool Update(int id, ProductUpsertRequest request);
    bool Delete(int id);
}

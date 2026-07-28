using tms_template_net8.Models.DTOs.Product;

namespace tms_template_net8.Data.Repositories;

public interface IProductRepository
{
    IReadOnlyList<ProductItem> GetAll();
    ProductItem? GetById(int id);
    ProductItem Add(ProductItem product);
    bool Update(int id, ProductItem product);
    bool Delete(int id);
}

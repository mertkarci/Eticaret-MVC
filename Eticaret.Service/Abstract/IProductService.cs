using Eticaret.Core.Entities;

namespace Eticaret.Service.Abstract;

public interface IProductService
{
    Task<(List<Product> Products, List<Category> Categories, List<Brand> Brands)> GetAllProductsAndFiltersAsync();
    Task<List<Product>> FilterAllProductsAsync(List<int> selectedBrands, List<int> selectedCategories, decimal? minPrice, decimal? maxPrice, string searchTerm);
}
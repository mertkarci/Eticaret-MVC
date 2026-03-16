using Eticaret.Core.Entities;

namespace Eticaret.Service.Abstract;

public interface ICategoryService
{
    Task<(Category Category, List<Category> SubCategories, List<Product> AllProducts, List<Brand> AvailableBrands)?> GetCategoryDetailsBySlugAsync(string slug);
    
    Task<List<Product>> FilterCategoryProductsAsync(int categoryId, List<int> selectedBrands, List<int> selectedCategories, decimal? minPrice, decimal? maxPrice, string searchTerm);
}
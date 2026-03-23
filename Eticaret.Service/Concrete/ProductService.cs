using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.Service.Concrete;

public class ProductService : IProductService
{
    private readonly IService<Product> _productService;
    private readonly IService<Category> _categoryService;
    private readonly IService<Brand> _brandService;

    public ProductService(IService<Product> productService, IService<Category> categoryService, IService<Brand> brandService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _brandService = brandService;
    }

    public async Task<(List<Product> Products, List<Category> Categories, List<Brand> Brands)> GetAllProductsAndFiltersAsync()
    {
        var products = await _productService.GetQueryable().Include(p => p.Brand).Include(p => p.Category).ToListAsync();
        var categories = await _categoryService.GetAllAsync();
        var brands = await _brandService.GetAllAsync();

        return (products, categories, brands);
    }

    public async Task<List<Product>> FilterAllProductsAsync(List<int> selectedBrands, List<int> selectedCategories, decimal? minPrice, decimal? maxPrice, string searchTerm)
    {
        var query = _productService.GetQueryable().Include(p => p.Brand).Include(p => p.Category).AsQueryable();

        if (selectedBrands != null && selectedBrands.Any())
            query = query.Where(p => p.BrandId.HasValue && selectedBrands.Contains(p.BrandId.Value));

        if (selectedCategories != null && selectedCategories.Any())
            query = query.Where(p => p.CategoryId.HasValue && selectedCategories.Contains(p.CategoryId.Value));

        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower().Trim();
            query = query.Where(p => p.Name != null && p.Name.ToLower().Contains(searchTerm));
        }

        return await query.ToListAsync();
    }
}
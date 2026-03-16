using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.Service.Concrete;

public class CategoryService : ICategoryService
{
    private readonly IService<Category> _categoryService;

    public CategoryService(IService<Category> categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<(Category Category, List<Category> SubCategories, List<Product> AllProducts, List<Brand> AvailableBrands)?> GetCategoryDetailsBySlugAsync(string slug)
    {
        var category = await _categoryService.GetQueryable()
            .Include(p => p.Products).ThenInclude(pr => pr.Brand)
            .FirstOrDefaultAsync(m => m.Slug == slug);

        if (category == null) return null;

        var subCategories = await _categoryService.GetQueryable()
            .Where(c => c.ParentId == category.Id)
            .Include(c => c.Products).ThenInclude(pr => pr.Brand)
            .ToListAsync();

        var allProducts = category.Products?.ToList() ?? new List<Product>();
        foreach (var sub in subCategories)
        {
            if (sub.Products != null) allProducts.AddRange(sub.Products);
        }

        var availableBrands = allProducts
            .Where(p => p.Brand != null)
            .Select(p => p.Brand)
            .DistinctBy(b => b.Id)
            .ToList();

        return (category, subCategories, allProducts, availableBrands);
    }

    public async Task<List<Product>> FilterCategoryProductsAsync(int categoryId, List<int> selectedBrands, List<int> selectedCategories, decimal? minPrice, decimal? maxPrice, string searchTerm)
    {
        var category = await _categoryService.GetQueryable()
            .Include(p => p.Products).ThenInclude(pr => pr.Brand)
            .FirstOrDefaultAsync(m => m.Id == categoryId);

        if (category == null) return new List<Product>();

        var subCategories = await _categoryService.GetQueryable()
            .Where(c => c.ParentId == categoryId)
            .Include(c => c.Products).ThenInclude(pr => pr.Brand)
            .ToListAsync();

        var allProducts = category.Products?.ToList() ?? new List<Product>();
        foreach (var sub in subCategories)
        {
            if (sub.Products != null) allProducts.AddRange(sub.Products);
        }

        // Marka Filtresi
        if (selectedBrands != null && selectedBrands.Any())
            allProducts = allProducts.Where(p => p.Brand != null && selectedBrands.Contains(p.Brand.Id)).ToList();

        // Kategori Filtresi
        if (selectedCategories != null && selectedCategories.Any())
            allProducts = allProducts.Where(p => p.CategoryId.HasValue && selectedCategories.Contains(p.CategoryId.Value)).ToList();

        // Fiyat Filtresi
        if (minPrice.HasValue) allProducts = allProducts.Where(p => p.Price >= minPrice.Value).ToList();
        if (maxPrice.HasValue) allProducts = allProducts.Where(p => p.Price <= maxPrice.Value).ToList();

        // Arama Filtresi
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower().Trim();
            allProducts = allProducts.Where(p => p.Name != null && p.Name.ToLower().Contains(searchTerm)).ToList();
        }

        return allProducts;
    }
}
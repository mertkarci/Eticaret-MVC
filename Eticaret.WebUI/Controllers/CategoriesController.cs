using Eticaret.Service.Abstract;
using Eticaret.WebUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace Eticaret.WebUI.Controllers
{
    [EnableRateLimiting("ProductLimit")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;


        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;

        }
        [Route("/kategoriler/{**slug}")]
        [Route("/kategoriler/{mainSlug}/{slug}")]
        public async Task<IActionResult> IndexAsync(string slug, string? mainSlug = null)
        {
            if (string.IsNullOrEmpty(slug)) return NotFound();

            var result = await _categoryService.GetCategoryDetailsBySlugAsync(slug);

            if (result == null) return NotFound();

            var viewModel = new CategoryFilterViewModel
            {
                CurrentCategory = result.Value.Category,
                SubCategories = result.Value.SubCategories,
                FilteredProducts = result.Value.AllProducts,
                AvailableBrands = result.Value.AvailableBrands
            };

            return View(viewModel);
        }

        [HttpPost]
        [EnableRateLimiting("ProductLimit")]

        public async Task<IActionResult> FilterProducts(int? categoryId, List<int> selectedBrands, List<int> selectedCategories, decimal? minPrice, decimal? maxPrice, string searchTerm)
        {
            int safeCategoryId = categoryId ?? 0;

            var filteredProducts = await _categoryService.FilterCategoryProductsAsync(safeCategoryId, selectedBrands, selectedCategories, minPrice, maxPrice, searchTerm);

            return PartialView("_ProductListPartial", filteredProducts);
        }
    }
}
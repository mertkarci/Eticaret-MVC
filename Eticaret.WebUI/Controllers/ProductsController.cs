using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Eticaret.WebUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.WebUI.Controllers
{
    [EnableRateLimiting("ProductLimit")]
    public class ProductsController : Controller
    {
        private readonly IService<Product> _service;
        private readonly IService<Brand> _brandService;
        private readonly IService<Category> _categoryService;

        public ProductsController(IService<Product> service, IService<Brand> brandService, IService<Category> categoryService)
        {
            _service = service;
            _brandService = brandService;
            _categoryService = categoryService;
        }

        [Route("urun")]
        [EnableRateLimiting("ProductLimit")]
        public async Task<IActionResult> Index(string q = "")
        {
            var productQuery = _service.GetQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                string query = q.ToLower();
                productQuery = productQuery.Where(p => p.isActive && p.Name.ToLower().Contains(query));
            }
            else
            {
                productQuery = productQuery.Where(p => p.isActive);
            }

            var products = await productQuery
                                .Include(p => p.Brand)
                                .Include(p => p.Category)
                                .ToListAsync();

            var brands = await _brandService.GetAllAsync(b => b.isActive);
            var allCategories = await _categoryService.GetAllAsync(c => c.isActive);

            var model = new CategoryFilterViewModel
            {
                CurrentCategory = new Category { Name = "Tüm Ürünler", Id = 0 }, // Dummy kategori
                SubCategories = allCategories.ToList(),
                FilteredProducts = products,
                AvailableBrands = brands.ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> FilterProducts(List<int> selectedBrands, List<int> selectedCategories, decimal? minPrice, decimal? maxPrice, string searchTerm, string sort = "recommended")
        {
            var productQuery = _service.GetQueryable().Where(p => p.isActive);

            if (selectedBrands != null && selectedBrands.Any())
                productQuery = productQuery.Where(p => p.BrandId != 0 && selectedBrands.Contains(p.BrandId));

            if (selectedCategories != null && selectedCategories.Any())
            {
                var allCategories = await _categoryService.GetAllAsync();
                var categoryIdsToSearch = new HashSet<int>(selectedCategories);

                bool addedNew;
                do
                {
                    addedNew = false;
                    // Şu anki listemizde ParentId'si bulunan ama kendisi listede olmayan kategorileri bul
                    var subCatIds = allCategories
                        .Where(c => categoryIdsToSearch.Contains(c.ParentId) && !categoryIdsToSearch.Contains(c.Id))
                        .Select(c => c.Id)
                        .ToList();

                    if (subCatIds.Any())
                    {
                        foreach (var id in subCatIds) categoryIdsToSearch.Add(id);
                        addedNew = true; // Yeni alt kategoriler eklendi, onların da alt kategorisi olabilir diye döngüyü tekrarla
                    }
                } while (addedNew);

                var finalCategoryIds = categoryIdsToSearch.ToList();
                productQuery = productQuery.Where(p => p.CategoryId.HasValue && finalCategoryIds.Contains(p.CategoryId.Value));
            }

            if (minPrice.HasValue) productQuery = productQuery.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) productQuery = productQuery.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();
                productQuery = productQuery.Where(p => p.Name.ToLower().Contains(searchTerm));
            }

            productQuery = sort switch
            {
                "price_asc" => productQuery.OrderBy(p => p.Price),
                "price_desc" => productQuery.OrderByDescending(p => p.Price),
                "newest" => productQuery.OrderByDescending(p => p.Id),
                _ => productQuery
            };

            var products = await productQuery.Include(p => p.Brand).Include(p => p.Category).ToListAsync();

            return PartialView("_ProductListPartial", products);
        }

        [Route("/urun/{slug}", Name = "ProductDetail")]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return RedirectToAction(nameof(Index));
            }
            var queryable = _service.GetQueryable();
            if (queryable == null)
            {

                return Content("Hata: Veritabanı sorgu kaynağı (GetQueryable) boş döndü.");
            }

            var product = await queryable
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (product == null) return NotFound();


            var relatedProducts = await queryable
                .Where(p => p.isActive && p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4)
                .ToListAsync() ?? new List<Product>();

            var model = new ProductDetailViewModel()
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(model);
        }
    }

}

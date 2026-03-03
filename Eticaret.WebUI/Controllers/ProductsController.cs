using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Eticaret.WebUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 

namespace Eticaret.WebUI.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IService<Product> _service;
        private readonly IService<Brand> _brandService;
        private readonly IService<Category> _categoryService;

        // TEK VE GÜNCEL YAPICI METOT (CONSTRUCTOR) BU OLMALI:
        public ProductsController(IService<Product> service, IService<Brand> brandService, IService<Category> categoryService)
        {
            _service = service;
            _brandService = brandService;
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index(string q = "")
        {
            // 1. ADIM: Servisten "Henüz Çalışmamış" sorguyu (IQueryable) alıyoruz.
            var productQuery = _service.GetQueryable();

            // 2. ADIM: Filtreleme (Arama mantığı)
            if (!string.IsNullOrEmpty(q))
            {
                string query = q.ToLower();
                // İsmi aranan kelimeyi içeren VE Aktif olanlar
                productQuery = productQuery.Where(p => p.isActive && p.Name.ToLower().Contains(query));
            }
            else
            {
                // Arama yoksa sadece aktifleri getir
                productQuery = productQuery.Where(p => p.isActive);
            }

            var products = await productQuery
                                .Include(p => p.Brand)
                                .Include(p => p.Category)
                                .ToListAsync();

            // Sidebar için verileri çek
            var brands = await _brandService.GetAllAsync(b => b.isActive);
            var categories = await _categoryService.GetAllAsync(c => c.isActive && c.ParentId == 0); // Ana kategoriler

            // ViewModel oluştur
            var model = new CategoryFilterViewModel
            {
                CurrentCategory = new Category { Name = "Tüm Ürünler", Id = 0 }, // Dummy kategori
                SubCategories = categories.ToList(),
                FilteredProducts = products,
                AvailableBrands = brands.ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> FilterProducts(List<int> selectedBrands, List<int> selectedCategories, decimal? minPrice, decimal? maxPrice, string searchTerm)
        {
            var productQuery = _service.GetQueryable().Where(p => p.isActive);

            // Marka Filtresi
            if (selectedBrands != null && selectedBrands.Any())
                productQuery = productQuery.Where(p => p.BrandId != 0 && selectedBrands.Contains(p.BrandId));

            // Kategori Filtresi
            if (selectedCategories != null && selectedCategories.Any())
                productQuery = productQuery.Where(p => p.CategoryId.HasValue && selectedCategories.Contains(p.CategoryId.Value));

            // Fiyat Filtresi
            if (minPrice.HasValue) productQuery = productQuery.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) productQuery = productQuery.Where(p => p.Price <= maxPrice.Value);

            // Arama Filtresi
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();
                productQuery = productQuery.Where(p => p.Name.ToLower().Contains(searchTerm));
            }

            var products = await productQuery.Include(p => p.Brand).Include(p => p.Category).ToListAsync();

            return PartialView("_ProductListPartial", products);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Ürünü Detaylarıyla Çekme
            var product = await _service.GetQueryable()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // 2. Benzer Ürünleri Çekme (Related Products)
            // Aynı kategorideki, kendisi haricindeki aktif 4 ürünü getir.
            var relatedProducts = await _service.GetQueryable()
                .Where(p => p.isActive && p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4)
                .ToListAsync();

            // ViewModel Doldurma
            var model = new ProductDetailViewModel()
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(model);
        }
    }
}

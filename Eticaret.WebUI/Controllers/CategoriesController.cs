using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Eticaret.WebUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.WebUI.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly IService<Category> _service;
  

        public CategoriesController(IService<Category> service, IService<Brand> brandService)
        {
            _service = service;

        }
        [Route("/kategoriler/{**slug}")]
        [Route("/kategoriler/{mainSlug}/{slug}")]
        public async Task<IActionResult> IndexAsync(string slug, string? mainSlug = null)
        {
            if (string.IsNullOrEmpty(slug)) return NotFound();
            

            // 1. Ana kategoriyi SLUG üzerinden bul ve ürünlerini çek
            var category = await _service.GetQueryable()
                .Include(p => p.Products).ThenInclude(pr => pr.Brand)
                .FirstOrDefaultAsync(m => m.Slug == slug); // Id yerine Slug ile arıyoruz

            if (category == null) return NotFound();

            // 2. Alt kategorileri çek (ParentId hala ana kategorinin Id'si olmalı)
            var subCategories = await _service.GetQueryable()
                .Where(c => c.ParentId == category.Id)
                .Include(c => c.Products).ThenInclude(pr => pr.Brand)
                .ToListAsync();

            // 3. Tüm ürünleri bir havuzda topla
            var allProducts = category.Products.ToList();
            foreach (var sub in subCategories)
            {
                if (sub.Products != null) allProducts.AddRange(sub.Products);
            }

            var viewModel = new CategoryFilterViewModel
            {
                CurrentCategory = category,
                SubCategories = subCategories,
                FilteredProducts = allProducts,
                AvailableBrands = allProducts
                    .Where(p => p.Brand != null)
                    .Select(p => p.Brand)
                    .DistinctBy(b => b.Id)
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> FilterProducts(int categoryId, List<int> selectedBrands, List<int> selectedCategories, decimal? minPrice, decimal? maxPrice, string searchTerm)
        {
            // 1. Ürünleri tekrar birleştir
            var category = await _service.GetQueryable()
                .Include(p => p.Products).ThenInclude(pr => pr.Brand)
                .FirstOrDefaultAsync(m => m.Id == categoryId);

            var subCategories = await _service.GetQueryable()
                .Where(c => c.ParentId == categoryId)
                .Include(c => c.Products).ThenInclude(pr => pr.Brand)
                .ToListAsync();

            var allProducts = category.Products.ToList();
            foreach (var sub in subCategories)
            {
                if (sub.Products != null) allProducts.AddRange(sub.Products);
            }

            // --- FİLTRELEME ---

            // Marka Filtresi
            if (selectedBrands != null && selectedBrands.Any())
                allProducts = allProducts.Where(p => p.Brand != null && selectedBrands.Contains(p.Brand.Id)).ToList();

            // Kategori Filtresi (int? hatası giderilmiş hali)
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

            // Partial View sadece ürün listesini (List<Product>) beklediği için onu gönderiyoruz
            return PartialView("_ProductListPartial", allProducts);
        }
    }
}
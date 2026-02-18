using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 

namespace Eticaret.WebUI.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IService<Product> _service;

        public ProductsController(IService<Product> service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index(string q = "")
        {
            // 1. ADIM: Servisten "Henüz Çalışmamış" sorguyu (IQueryable) alıyoruz.
            // _context.Products.AsQueryable() YERİNE:
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

            // 3. ADIM: İlişkili tabloları (Brand, Category) dahil et ve veritabanına git
            var result = await productQuery
                                .Include(p => p.Brand)
                                .Include(p => p.Category)
                                .ToListAsync(); // Veritabanı sorgusu BURADA çalışır

            return View(result);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Ürünü Detaylarıyla Çekme
            // _context... YERİNE _service.GetQueryable()...
            var product = await _service.GetQueryable()
                .Include(p => p.Brand)
                .Include(p => p.Category)
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
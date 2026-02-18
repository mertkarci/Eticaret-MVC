using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.WebUI.Controllers
{
    public class CategoriesController : Controller
    {
        // private readonly DatabaseContext _context;

        // public CategoriesController(DatabaseContext context)
        // {
        //     _context = context;
        // }

        private readonly IService<Category> _service;

        public CategoriesController(IService<Category> service)
        {
            _service = service;

        }
        public async Task<IActionResult> IndexAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. ADIM: Tıklanan kategoriyi ve ona DOĞRUDAN bağlı ürünleri çek
            var category = await _service.GetQueryable()
                .Include(p => p.Products) 
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            // 2. ADIM: Bu kategoriye bağlı ALT KATEGORİLERİ (Çocukları) bul
            // Ve o çocukların içindeki ürünleri de (Include ile) getir.
            var subCategories = await _service.GetQueryable()
                .Where(c => c.ParentId == id) // Babası bu kategori olanları bul
                .Include(c => c.Products)     // Onların ürünlerini de yükle
                .ToListAsync();

            // 3. ADIM: Çocukların ürünlerini, ana kategorinin ürün listesine ekle (Hepsini birleştir)
            foreach (var sub in subCategories)
            {
                if (sub.Products != null)
                {
                    // Listeyi genişletiyoruz: Mevcut ürünlerin yanına alt kategori ürünlerini ekle
                    foreach (var product in sub.Products)
                    {
                        category.Products.Add(product);
                    }
                }
            }

            return View(category);
        }

    }
}

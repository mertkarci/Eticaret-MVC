using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Eticaret.Core.Entities;
using Eticaret.Data;
using Eticaret.WebUI.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Eticaret.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class CategoriesController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(DatabaseContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.ToListAsync());
        }

        // GET: Admin/Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Admin/Categories/Create
        public async Task<IActionResult> CreateAsync()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, IFormFile? Image)
        {
            if (ModelState.IsValid)
            {
                if (Image != null)
                {
                    category.Image = await FileHelper.FileLoaderAsync(Image, "/img/categories/");
                }

                // EKSİK OLAN SLUG ÜRETME MANTIĞI EKLENDİ
                string baseSlug = UrlHelper.FriendlyUrl(category.Name ?? "");
                string finalSlug = baseSlug;
                int counter = 1;

                while (await _context.Categories.AnyAsync(c => c.Slug == finalSlug))
                {
                    finalSlug = $"{baseSlug}-{counter++}";
                }
                category.Slug = finalSlug;
                _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {CategoryName} isimli yeni bir kategori oluşturdu.", User.Identity.Name, category.Name);
                _context.Add(category);
                await _context.SaveChangesAsync();

                // AREA YÖNLENDİRMESİ DÜZELTİLDİ
                return RedirectToAction(nameof(Index), new { area = "Admin" });
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", category.ParentId);
            return View(category);
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var categoryList = await _context.Categories.Where(c => c.Id != id).ToListAsync();
            ViewBag.Categories = new SelectList(categoryList, "Id", "Name", category.ParentId);

            return View(category);
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category, IFormFile? Image, bool cbResmiSil)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // FORM'DAN SLUG BOŞ GELİRSE DÜŞECEĞİ GÜVENLİK AĞI
                    if (string.IsNullOrWhiteSpace(category.Slug))
                    {
                        string baseSlug = UrlHelper.FriendlyUrl(category.Name ?? "");
                        string finalSlug = baseSlug;
                        int counter = 1;
                        while (await _context.Categories.AnyAsync(c => c.Slug == finalSlug && c.Id != category.Id))
                        {
                            finalSlug = $"{baseSlug}-{counter++}";
                        }
                        category.Slug = finalSlug;
                    }

                    if (cbResmiSil)
                    {
                        category.Image = string.Empty;
                        _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {CategoryName} isimli kategorinin görselini sildi.", User.Identity.Name, category.Name);
                    }
                    if (Image != null)
                    {
                        category.Image = await FileHelper.FileLoaderAsync(Image, "/img/categories/");
                    }
                    _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {CategoryName} isimli kategoriyi düzenledi.", User.Identity.Name, category.Name);
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {CategoryName} isimli kategoriyi düzenlerken bir hata oluştu.", User.Identity.Name, category.Name);
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // GÜNCELLEDİKTEN SONRA AYNI SAYFADA KALMASI VE AREA İÇİNDE TUTULMASI SAĞLANDI
                return RedirectToAction(nameof(Edit), new { id = category.Id, area = "Admin" });
            }

            var categoryList = await _context.Categories.Where(c => c.Id != id).ToListAsync();
            ViewBag.Categories = new SelectList(categoryList, "Id", "Name", category.ParentId);

            return View(category);
        }

        // GET: Admin/Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                if (!string.IsNullOrEmpty(category.Image))
                {
                    FileHelper.FileRemover(category.Image, "/wwwroot/img/categories/");
                }
                _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {CategoryName} isimli kategoriyi sildi.", User.Identity.Name, category.Name);
                _context.Categories.Remove(category);
            }

            await _context.SaveChangesAsync();

            // AREA YÖNLENDİRMESİ DÜZELTİLDİ
            return RedirectToAction(nameof(Index), new { area = "Admin" });
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }


    }
}
// [Route("Admin/Categories/GenerateCategorySlugs")]
// public async Task<IActionResult> GenerateCategorySlugs()
// {
//     // Veritabanındaki tüm kategorileri çekiyoruz
//     var categories = await _context.Categories.ToListAsync();
//     int updatedCount = 0;

//     foreach (var category in categories)
//     {
//         // İsimden SEO uyumlu link oluştur (Örn: "Ev Eşyası" -> "ev-esyasi")
//         string baseSlug = UrlHelper.FriendlyUrl(category.Name ?? "");
//         string finalSlug = baseSlug;
//         int counter = 1;

//         // Benzersizlik kontrolü: Eğer aynı slug varsa sonuna -1, -2 ekler
//         while (await _context.Categories.AnyAsync(c => c.Slug == finalSlug && c.Id != category.Id))
//         {
//             finalSlug = $"{baseSlug}-{counter++}";
//         }

//         category.Slug = finalSlug;
//         _context.Update(category);
//         updatedCount++;
//     }

//     await _context.SaveChangesAsync();

//     // İşlem bitince liste sayfasına mesajla dön
//     TempData["Message"] = $"{updatedCount} adet kategori başarıyla SEO uyumlu hale getirildi.";
//     return RedirectToAction(nameof(Index));
// }

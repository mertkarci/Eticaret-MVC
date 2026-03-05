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

        public CategoriesController(DatabaseContext context)
        {
            _context = context;
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
            // ViewBag.Kategoriler yerine ViewBag.Categories yaptık ki View ile tam eşleşsin.
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, IFormFile Image)
        {
            if (ModelState.IsValid)
            {
                if (Image != null)
                {
                    category.Image = await FileHelper.FileLoaderAsync(Image, "/img/categories/");
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Model hatalıysa sayfa geri dönerken dropdown boşalmasın diye tekrar listeyi gönderiyoruz
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

            // Kategori kendi kendisinin üst kategorisi olamaz (c.Id != id)
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
                    if (cbResmiSil)
                    {
                        category.Image = string.Empty;
                    }
                    if (Image != null)
                    {
                        category.Image = await FileHelper.FileLoaderAsync(Image, "/img/categories/");
                    }
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // İşlem başarısız olursa ve sayfa geri yüklenecekse, dropdown'ı tekrar doldur (Kendisi hariç)
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
                _context.Categories.Remove(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
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
    }
}
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
    public class ProductImagesController : Controller
    {
        private readonly DatabaseContext _context;

        public ProductImagesController(DatabaseContext context)
        {
            _context = context;
        }

        // GET: Admin/ProductImages
        public async Task<IActionResult> Index(int? ProductId)
        {
            var databaseContext = _context.ProductImages.Include(p => p.Product);
            if (ProductId.HasValue)
            {
                return View(await databaseContext.Where(p => p.ProductId == ProductId).ToListAsync());
            }
            return View(await databaseContext.ToListAsync());
        }

        // GET: Admin/ProductImages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productImage = await _context.ProductImages
                .Include(p => p.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (productImage == null)
            {
                return NotFound();
            }

            return View(productImage);
        }

        // GET: Admin/ProductImages/Create
        public IActionResult Create(string ProductId)
        {
            // Dropdown'da ID yerine ürün ismi (Name) görünmesi için düzeltildi
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", ProductId);
            return View();
        }

        // POST: Admin/ProductImages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // DÜZELTME 1: IFormFile parametresini CSHTML'deki name attribute'u ile aynı yaptık (Image)
        public async Task<IActionResult> Create(ProductImage productImage, IFormFile? Image)
        {
            // DÜZELTME 2: İlişkili tablodan (Product) dolayı ModelState'in patlamasını engelliyoruz
            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                productImage.Name = await FileHelper.FileLoaderAsync(Image, "/img/products/");
                _context.Add(productImage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", productImage.ProductId);
            return View(productImage);
        }

        // GET: Admin/ProductImages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productImage = await _context.ProductImages.FindAsync(id);
            if (productImage == null)
            {
                return NotFound();
            }
            // Dropdown'da ID yerine ürün ismi (Name) görünmesi için düzeltildi
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", productImage.ProductId);
            return View(productImage);
        }

        // POST: Admin/ProductImages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductImage productImage, IFormFile? Image, bool cbResmiSil = false)
        {
            if (id != productImage.Id) return NotFound();

            ModelState.Remove("Product");
            ModelState.Remove("Name");

            if (ModelState.IsValid)
            {
                try
                {
                    if (cbResmiSil)
                    {
                        productImage.Name = string.Empty;
                    }
                    if (Image != null)
                    {
                        productImage.Name = await FileHelper.FileLoaderAsync(Image, "/img/products/");
                    }

                    _context.Update(productImage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductImageExists(productImage.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine("GİZLİ HATA: " + error.ErrorMessage);
            }

            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Name", productImage.ProductId);
            return View(productImage);
        }

        // GET: Admin/ProductImages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productImage = await _context.ProductImages
                .Include(p => p.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (productImage == null)
            {
                return NotFound();
            }

            return View(productImage);
        }

        // POST: Admin/ProductImages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productImage = await _context.ProductImages.FindAsync(id);
            if (productImage != null)
            {
                _context.ProductImages.Remove(productImage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductImageExists(int id)
        {
            return _context.ProductImages.Any(e => e.Id == id);
        }
    }
}
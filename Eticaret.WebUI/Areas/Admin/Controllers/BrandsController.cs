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
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.AspNetCore.Authorization;
using Serilog;

namespace Eticaret.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class BrandsController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<AddressesController> _logger;
        public BrandsController(DatabaseContext context, ILogger<AddressesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Brands
        public async Task<IActionResult> Index()
        {
            return View(await _context.Brands.ToListAsync());
        }

        // GET: Admin/Brands/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // GET: Admin/Brands/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Brands/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand, IFormFile? Logo)
        {
            if (ModelState.IsValid)
            {
                brand.Logo = await FileHelper.FileLoaderAsync(Logo, "/img/brands/");
                _context.Add(brand);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {BrandName} isimli yeni bir marka oluşturdu.", User.Identity.Name, brand.Name);
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: Admin/Brands/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: Admin/Brands/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand, IFormFile? Logo, bool cbResmiSil)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (cbResmiSil)
                    {
                        _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {BrandName} isimli markanın görselini sildi.", User.Identity.Name, brand.Name);
                        brand.Logo = string.Empty;
                    }
                    if (Logo != null)
                    {

                        brand.Logo = await FileHelper.FileLoaderAsync(Logo, "/img/brands/");
                    }
                    _context.Update(brand);
                    _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {BrandName} isimli markayı düzenledi.", User.Identity.Name, brand.Name);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.Id))
                    {
                        _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {BrandName} isimli markayı düzenlerken bir hata oluştu.", User.Identity.Name, brand.Name);

                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: Admin/Brands/Delete/5
        public async Task<IActionResult> Delete(int? id, IFormFile? Logo)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // POST: Admin/Brands/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                // 1. İLİŞKİLİ ÜRÜNLERİ BOŞA ÇIKAR (Markayı NULL yap)
                var relatedProducts = await _context.Products.Where(p => p.BrandId == id).ToListAsync();
                foreach (var product in relatedProducts)
                {
                    product.BrandId = null;
                }

                if (!string.IsNullOrEmpty(brand.Logo))
                {
                    FileHelper.FileRemover(brand.Logo, "/wwwroot/img/brands/");
                }
                _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {BrandName} isimli markayı sildi.", User.Identity.Name, brand.Name);
                _context.Brands.Remove(brand);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }
    }
}

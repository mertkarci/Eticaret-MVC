using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Eticaret.Core.Entities;
using Eticaret.Data;
using Microsoft.AspNetCore.Authorization;
using Eticaret.WebUI.Areas.Admin.Models;

namespace Eticaret.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class AppUsersController : Controller
    {
        private readonly DatabaseContext _context;

        public AppUsersController(DatabaseContext context)
        {
            _context = context;
        }

        // GET: Admin/AppUsers
        public async Task<IActionResult> Index()
        {
            return View(await _context.AppUsers.ToListAsync());
        }

        // GET: Admin/AppUsers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Kullanıcıyı bul
            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appUser == null)
            {
                return NotFound();
            }

            // 2. Kullanıcının Siparişlerini bul
            // Sipariş nesnesinin içindeki AppUserId'nin, bizim aradığımız id'ye eşit olanlarını listele
            var orders = await _context.Orders
                .Where(o => o.AppUserId == id)
                .ToListAsync();

            // 3. Kullanıcının Adreslerini bul
            // Adres nesnesinin içindeki AppUserId'nin, bizim aradığımız id'ye eşit olanlarını listele
            var addresses = await _context.Addresses
                .Where(a => a.AppUserId == id)
                .ToListAsync();

            // 4. Hepsini ViewModel kutusuna doldur ve View'a gönder
            // (Bunu yapmazsak View'da sekmelere verileri dağıtamayız)
            var model = new UserDetailsViewModel
            {
                User = appUser,
                Orders = orders,
                Addresses = addresses
            };

            return View(model);
        }

        // GET: Admin/AppUsers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/AppUsers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create(AppUser appUser)
        {
            if (ModelState.IsValid)
            {
                _context.Add(appUser);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(appUser);
        }

        // GET: Admin/AppUsers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appUser = await _context.AppUsers.FindAsync(id);
            if (appUser == null)
            {
                return NotFound();
            }
            return View(appUser);
        }

        // POST: Admin/AppUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Edit(int id, AppUser appUser)
        {
            if (id != appUser.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppUserExists(appUser.Id))
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
            return View(appUser);
        }

        // GET: Admin/AppUsers/Delete/5
        // GET: Admin/AppUsers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appUser == null)
            {
                return NotFound();
            }

            return View(appUser);
        }

        // POST: Admin/AppUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id, bool isHardDelete = false)
        {
            var appUser = await _context.AppUsers.FindAsync(id);

            if (appUser != null)
            {
                if (isHardDelete)
                {
                    // --- 1. SEÇENEK: HARD DELETE (Kalıcı Silme) ---
                    // FOREIGN KEY hatası almamak için önce bu kullanıcıya ait diğer verileri silmeliyiz.

                    // Kullanıcının adreslerini bul ve sil (DbSet adın 'Addresses' değilse kendine göre düzelt)
                    var userAddresses = _context.Addresses.Where(a => a.AppUserId == id);
                    if (userAddresses.Any())
                    {
                        _context.Addresses.RemoveRange(userAddresses);
                    }

                    // Kullanıcının siparişlerini bul ve sil (DbSet adın 'Orders' değilse düzelt)
                    // Not: Siparişlerin içindeki OrderLines tabloları cascade delete ile otomatik silinmiyorsa, onları da bulup silmen gerekebilir.
                    var userOrders = _context.Orders.Where(o => o.AppUserId == id);
                    if (userOrders.Any())
                    {
                        _context.Orders.RemoveRange(userOrders);
                    }

                    // Bağlantılı verileri temizledikten sonra artık kullanıcıyı kökünden silebiliriz.
                    _context.AppUsers.Remove(appUser);
                }
                else
                {
                    // --- 2. SEÇENEK: SOFT DELETE (Pasife Çekme) ---
                    // Veritabanından silmek yerine durumu pasif yapıyoruz. Muhasebe ve sipariş geçmişi bozulmaz.
                    appUser.isActive = false;

                    _context.AppUsers.Update(appUser);
                }

                // Yapılan işlemi (Silme veya Güncelleme) veritabanına kaydet
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AppUserExists(int id)
        {
            return _context.AppUsers.Any(e => e.Id == id);
        }
    }
}

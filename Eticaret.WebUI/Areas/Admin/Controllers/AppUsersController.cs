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
        private readonly ILogger<AppUsersController> _logger;

        public AppUsersController(DatabaseContext context, ILogger<AppUsersController> logger)
        {
            _context = context;
            _logger = logger;
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

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appUser == null)
            {
                return NotFound();
            }


            var orders = await _context.Orders
                .Where(o => o.AppUserId == id)
                .ToListAsync();

            var addresses = await _context.Addresses
                .Where(a => a.AppUserId == id)
                .ToListAsync();


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

        [HttpPost]
        public async Task<IActionResult> Create(AppUser appUser)
        {
            if (ModelState.IsValid)
            {
                _context.Add(appUser);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {UserId} ID'li yeni bir kullanıcı oluşturdu.", User.Identity.Name, appUser.Id);
                
                return RedirectToAction(nameof(Index), new { area = "Admin" });
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
                    
                    _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {UserId} ID'li kullanıcının bilgilerini güncelledi.", User.Identity.Name, appUser.Id);
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
                return RedirectToAction(nameof(Index), new { area = "Admin" });
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

                    var userAddresses = _context.Addresses.Where(a => a.AppUserId == id);
                    if (userAddresses.Any())
                    {
                        _context.Addresses.RemoveRange(userAddresses);
                    }

                    var userOrders = _context.Orders.Where(o => o.AppUserId == id);
                    if (userOrders.Any())
                    {
                        _context.Orders.RemoveRange(userOrders);
                    }

                    _context.AppUsers.Remove(appUser);
                }
                else
                {

                    appUser.isActive = false;

                    _context.AppUsers.Update(appUser);
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {UserId} ID'li kullanıcıyı sildi. (Kalıcı Silme: {IsHardDelete})", User.Identity.Name, id, isHardDelete);
            }

            return RedirectToAction(nameof(Index), new { area = "Admin" });
        }

        private bool AppUserExists(int id)
        {
            return _context.AppUsers.Any(e => e.Id == id);
        }
    }
}

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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace Eticaret.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class NotificationController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(DatabaseContext context, ILogger<NotificationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Notification
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Notification.ToListAsync());
        }

        // GET: Admin/Notification/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notification = await _context.Notification
                .FirstOrDefaultAsync(m => m.Id == id);
            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
        }

        // GET: Admin/Notification/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Notification/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Notification notification, IFormFile? Image)
        {
            if (ModelState.IsValid)
            {
                if (Image != null && notification.isString)
                {
                    notification.Image = await FileHelper.FileLoaderAsync(Image, "/img/popup/");
                }

                _context.Add(notification);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {PopupName} isimli yeni bir popup bildirim oluşturdu.", User.Identity.Name, notification.Name);
                return RedirectToAction(nameof(Index));
            }
            return View(notification);
        }

        // GET: Admin/Notification/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notification = await _context.Notification.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }
            return View(notification);
        }

        // POST: Admin/Notification/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Notification notification, IFormFile? Image, bool cbResmiSil = false)
        {
            if (id != notification.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (cbResmiSil)
                    {
                        notification.Image = string.Empty;
                        _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {PopupName} isimli popup bildirim'nin görselini sildi.", User.Identity.Name, notification.Name);
                    }
                    if (Image != null && notification.isString)
                    {
                        notification.Image = await FileHelper.FileLoaderAsync(Image, "/img/popup/");
                    }

                    _context.Update(notification);
                    _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {PopupName} isimli popup bildirim'ini düzenledi.", User.Identity.Name, notification.Name);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NotificationExists(notification.Id))
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
            return View(notification);
        }

        // GET: Admin/Notification/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notification = await _context.Notification
                .FirstOrDefaultAsync(m => m.Id == id);
            if (notification == null)
            {
                return NotFound();
            }

            return View(notification);
        }

        // POST: Admin/Notification/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notification = await _context.Notification.FindAsync(id);
            if (notification != null)
            {
                if (!string.IsNullOrEmpty(notification.Image))
                {
                    FileHelper.FileRemover(notification.Image, "/wwwroot/img/popup/");
                }

                _context.Notification.Remove(notification);
                _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {PopupName} isimli popup bildirim'ini sildi.", User.Identity.Name, notification.Name);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NotificationExists(int id)
        {
            return _context.Notification.Any(e => e.Id == id);
        }
    }
}

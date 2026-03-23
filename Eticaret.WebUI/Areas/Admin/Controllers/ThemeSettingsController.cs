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

namespace Eticaret.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class ThemeSettingsController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<ThemeSettingsController> _logger;


        public ThemeSettingsController(DatabaseContext context, ILogger<ThemeSettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/ThemeSettings
        public async Task<IActionResult> Index()
        {
            return View(await _context.ThemeSettings.ToListAsync());
        }

        // GET: Admin/ThemeSettings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var themeSetting = await _context.ThemeSettings
                .FirstOrDefaultAsync(m => m.Id == id);
            if (themeSetting == null)
            {
                return NotFound();
            }

            return View(themeSetting);
        }

        // GET: Admin/ThemeSettings/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/ThemeSettings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ThemeSetting themeSetting)
        {
            if (ModelState.IsValid)
            {
                _context.Add(themeSetting);
                if (themeSetting.IsActive)
                {
                    _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {ThemeName} adlı yeni bir tema oluşturdu ve aktif oldu.", User.Identity.Name, themeSetting.Name);
                }
                else
                {
                    _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {ThemeName} adlı yeni bir tema oluşturdu.", User.Identity.Name, themeSetting.Name);
                }
                await CheckForTheme(themeSetting);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(themeSetting);
        }

        // GET: Admin/ThemeSettings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var themeSetting = await _context.ThemeSettings.FindAsync(id);
            if (themeSetting == null)
            {
                return NotFound();
            }
            return View(themeSetting);
        }

        // POST: Admin/ThemeSettings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ThemeSetting themeSetting)
        {
            if (id != themeSetting.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(themeSetting);
                    if (themeSetting.IsActive)
                    {
                        _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {ThemeName} adlı temayı aktifleştirdi.", User.Identity.Name, themeSetting.Name);
                    }
                    else
                    {
                        _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {ThemeName} adlı temayi düzenledi.", User.Identity.Name, themeSetting.Name);
                    }
                    await CheckForTheme(themeSetting);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThemeSettingExists(themeSetting.Id))
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
            return View(themeSetting);
        }

        // GET: Admin/ThemeSettings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var themeSetting = await _context.ThemeSettings
                .FirstOrDefaultAsync(m => m.Id == id);
            if (themeSetting == null)
            {
                return NotFound();
            }

            return View(themeSetting);
        }

        // POST: Admin/ThemeSettings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var themeSetting = await _context.ThemeSettings.FindAsync(id);
            if (themeSetting != null)
            {
                _context.ThemeSettings.Remove(themeSetting);
                _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, {ThemeName} adlı temayı sildi.", User.Identity.Name, themeSetting.Name);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ThemeSettingExists(int id)
        {
            return _context.ThemeSettings.Any(e => e.Id == id);
        }
        private async Task CheckForTheme(ThemeSetting themeSetting)
        {
            if (themeSetting.IsActive)
            {
                var activeThemes = await _context.ThemeSettings
                    .Where(x => x.IsActive && x.Id != themeSetting.Id)
                    .ToListAsync();

                foreach (var item in activeThemes)
                {
                    item.IsActive = false;
                    _context.Update(item);
                }
            }
        }
    }
}

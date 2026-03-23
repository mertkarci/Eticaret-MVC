using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Eticaret.Core.Entities;
using Eticaret.Data;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Authorization;

namespace Eticaret.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class SiteSettingsController : Controller
    {
        private readonly DatabaseContext _context;
        private readonly IMaintenanceService _maintenanceService;
        private readonly ILogger<SiteSettingsController> _logger;


        public SiteSettingsController(DatabaseContext context, IMaintenanceService maintenanceService, ILogger<SiteSettingsController> logger)
        {
            _context = context;
            _maintenanceService = maintenanceService;
            _logger = logger;
        }

        // GET: Admin/SiteSettings
        public async Task<IActionResult> Index()
        {
            var siteSettings = await _context.SiteSettings.FirstOrDefaultAsync();

            if (siteSettings == null)
            {
                siteSettings = new SiteSettings
                {
                    IsMaintenanceMode = false,
                    MaintenanceMessage = "Sitemiz şu anda güncellenmektedir. Lütfen daha sonra tekrar deneyiniz."
                };

                _context.SiteSettings.Add(siteSettings);
                await _context.SaveChangesAsync();
            }

            return View(siteSettings);
        }

        // POST: Admin/SiteSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SiteSettings model)
        {
            if (ModelState.IsValid)
            {
                var existingSettings = await _context.SiteSettings.FirstOrDefaultAsync();

                if (existingSettings != null)
                {
                    // Update existing record
                    existingSettings.IsMaintenanceMode = model.IsMaintenanceMode;
                    existingSettings.MaintenanceMessage = model.MaintenanceMessage;
                    _context.Update(existingSettings);
                }
                else
                {
                    _context.Add(model);
                }
                _logger.LogInformation("Kullanıcı İşlemi: {Admin} adlı yönetici, site ayarlarını güncelledi.", User.Identity.Name);
                await _context.SaveChangesAsync();

                _maintenanceService.SetMaintenanceMode(model.IsMaintenanceMode);

                TempData["Message"] = "Site ayarları başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}
using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting; // Added for Rate Limiting
using System.Security.Claims;

namespace Eticaret.WebUI.Controllers
{
    [Authorize]
    [Route("adreslerim")]
    public class MyAddressesController : Controller
    {
        private readonly IService<AppUser> _serviceAppUser;
        private readonly IService<Address> _serviceAddress;

        public MyAddressesController(IService<AppUser> serviceAppUser, IService<Address> serviceAddress)
        {
            _serviceAppUser = serviceAppUser;
            _serviceAddress = serviceAddress;
        }

        private async Task<AppUser?> GetCurrentUserAsync()
        {
            var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;
            
            if (string.IsNullOrEmpty(userGuidStr) || !Guid.TryParse(userGuidStr, out Guid parsedGuid))
                return null;

            return await _serviceAppUser.GetAsync(p => p.UserGuid == parsedGuid); 
        }


        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var appUser = await GetCurrentUserAsync();
            if (appUser == null) return RedirectToAction("SignIn", "Accounts");

            var model = await _serviceAddress.GetAllAsync(p => p.AppUserId == appUser.Id);
            return View(model);
        }

        [HttpGet("ekle")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("ekle")]
        [ValidateAntiForgeryToken] 
        [EnableRateLimiting("FormLimit")]

        public async Task<IActionResult> Create(Address address)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Lütfen formdaki hataları düzeltin.");
                return View(address);
            }

            try
            {
                var appUser = await GetCurrentUserAsync();
                if (appUser == null) return RedirectToAction("SignIn", "Accounts");

                address.AppUserId = appUser.Id;
                
                // Ensure new addresses get a fresh Guid if your DB doesn't generate it automatically
                if (address.AddressGuid == Guid.Empty) 
                    address.AddressGuid = Guid.NewGuid();

                await _serviceAddress.AddAsync(address); // Prefer AddAsync if your service supports it
                await _serviceAddress.SaveChangesAsync();
                
                TempData["Message"] = "Adres başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Kayıt sırasında sistemsel bir hata oluştu.");
                return View(address);
            }
        }

        [HttpGet("duzenle/{**name}")]
        public async Task<IActionResult> Edit(string name)
        {
            if (string.IsNullOrEmpty(name)) return RedirectToAction(nameof(Index));

            var appUser = await GetCurrentUserAsync();
            if (appUser == null) return RedirectToAction("SignIn", "Accounts");

            var model = await _serviceAddress.GetAsync(p => p.Title == name && p.AppUserId == appUser.Id);

            if (model == null) return NotFound("Adres bulunamadı!");
            
            return View(model);
        }

        [HttpPost("duzenle/{**name}")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("FormLimit")]

        public async Task<IActionResult> Edit(string name, [FromForm] string id, Address address)
        {
            if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out Guid parsedGuid)) 
                return BadRequest("Geçersiz işlem: ID bulunamadı veya bozuk.");

            var appUser = await GetCurrentUserAsync();
            if (appUser == null) return RedirectToAction("SignIn", "Accounts");

            var model = await _serviceAddress.GetAsync(p => p.AddressGuid == parsedGuid && p.AppUserId == appUser.Id);

            if (model == null) return NotFound("Adres bulunamadı veya bu adresi düzenleme yetkiniz yok!");

            try
            {
                // Update properties
                model.Title = address.Title;
                model.District = address.District;
                model.City = address.City;
                model.OpenAddress = address.OpenAddress;
                model.IsDeliveryAddress = address.IsDeliveryAddress;
                model.IsBillingAddress = address.IsBillingAddress;
                model.IsActive = address.IsActive;

                // Handle Default Address Logic
                if (model.IsDeliveryAddress || model.IsBillingAddress)
                {
                    var otherAddresses = await _serviceAddress.GetAllAsync(p => p.AppUserId == appUser.Id && p.Id != model.Id);
                    foreach (var otherAddress in otherAddresses)
                    {
                        if (model.IsDeliveryAddress) otherAddress.IsDeliveryAddress = false;
                        if (model.IsBillingAddress)  otherAddress.IsBillingAddress = false;
                        _serviceAddress.Update(otherAddress);
                    }
                }

                _serviceAddress.Update(model);
                await _serviceAddress.SaveChangesAsync();
                
                TempData["Message"] = "Adres başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Kayıt sırasında bir hata oluştu.");
                return View(model);
            }
        }

        [HttpGet("sil/{**name}")]
        public async Task<IActionResult> Delete(string name)
        {
            if (string.IsNullOrEmpty(name)) return RedirectToAction(nameof(Index));

            var appUser = await GetCurrentUserAsync();
            if (appUser == null) return RedirectToAction("SignIn", "Accounts");

            var model = await _serviceAddress.GetAsync(p => p.Title.ToLower() == name.ToLower() && p.AppUserId == appUser.Id);

            if (model == null) return NotFound("Adres bulunamadı!");
            
            return View(model);
        }

        [HttpPost("sil/{**name}")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("FormLimit")]
        public async Task<IActionResult> Delete(string name, [FromForm] string id)
        {
            if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out Guid parsedGuid)) 
                return BadRequest("Geçersiz ID.");

            var appUser = await GetCurrentUserAsync();
            if (appUser == null) return RedirectToAction("SignIn", "Accounts");

            var model = await _serviceAddress.GetAsync(p => p.AddressGuid == parsedGuid && p.AppUserId == appUser.Id);

            if (model == null) return NotFound("Adres bulunamadı veya silmeye yetkiniz yok.");

            try
            {
                _serviceAddress.Delete(model);
                await _serviceAddress.SaveChangesAsync();
                
                TempData["Message"] = "Adres başarıyla silindi.";
                return RedirectToAction(nameof(Index)); 
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Silme işlemi sırasında bir hata oluştu.");
                return View(model); 
            }
        }
    }
}
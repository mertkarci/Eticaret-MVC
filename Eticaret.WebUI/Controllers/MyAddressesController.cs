using System.Threading.Tasks;
using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Eticaret.WebUI.Controllers
{
    [Authorize]
    public class MyAddressesController : Controller
    {
        private readonly IService<AppUser> _serviceAppUser;
        private readonly IService<Address> _serviceAddress;

        public MyAddressesController(IService<AppUser> serviceAppUser, IService<Address> serviceAddress)
        {
            _serviceAppUser = serviceAppUser;
            _serviceAddress = serviceAddress;

        }
        public async Task<IActionResult> Index()
        {
            var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;

            if (string.IsNullOrEmpty(userGuidStr))
            {
                return NotFound("Oturumda UserGuid bulunamadı.");
            }

            // Karşılaştırmayı daha güvenli hale getirelim (büyük/küçük harf duyarlılığını ortadan kaldırarak)
            var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid.ToString().ToLower() == userGuidStr.ToLower());

            if (appUser == null)
            {
                return NotFound("Kullanıcı bulunamadı! Veritabanında bu Guid'e sahip bir kayıt olmayabilir.");
            }

            // MUTLAKA await ekle, yoksa model View'a gitmeden Task olarak kalır
            var model = await _serviceAddress.GetAllAsync(p => p.AppUserId == appUser.Id);

            return View(model);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Address address)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;

                    if (string.IsNullOrEmpty(userGuidStr))
                    {
                        return NotFound("Oturumda UserGuid bulunamadı.");
                    }

                    // Karşılaştırmayı daha güvenli hale getirelim (büyük/küçük harf duyarlılığını ortadan kaldırarak)
                    var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid.ToString().ToLower() == userGuidStr.ToLower());
                    if (appUser != null)
                    {
                        address.AppUserId = appUser.Id;
                        _serviceAddress.Add(address);
                        await _serviceAddress.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Hata Oluştu");
                }

            }
            ModelState.AddModelError("", "Kayıt Başarısız!");
            return View(address);
        }
        public async Task<IActionResult> Edit(string id)

        {
            var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;

            if (string.IsNullOrEmpty(userGuidStr))
            {
                return NotFound("Oturumda UserGuid bulunamadı.");
            }

            // Karşılaştırmayı daha güvenli hale getirelim (büyük/küçük harf duyarlılığını ortadan kaldırarak)
            var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid.ToString().ToLower() == userGuidStr.ToLower());

            if (appUser == null)
            {
                return NotFound("Kullanıcı bulunamadı! Veritabanında bu Guid'e sahip bir kayıt olmayabilir.");
            }
            var model = await _serviceAddress.GetAsync(p => p.AddressGuid.ToString().ToLower() == id.ToLower() && p.AppUserId == appUser.Id);
            if (model != null)
            {
                return View(model);
            }
            else
            {
                return NotFound("Adres bulunamadı! Veritabanında bu Guid'e sahip bir kayıt olmayabilir.");

            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Address address)
        {
            var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;

            if (string.IsNullOrEmpty(userGuidStr))
            {
                return NotFound("Oturumda UserGuid bulunamadı.");
            }

            // Karşılaştırmayı daha güvenli hale getirelim (büyük/küçük harf duyarlılığını ortadan kaldırarak)
            var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid.ToString().ToLower() == userGuidStr.ToLower());

            if (appUser == null)
            {
                return NotFound("Kullanıcı bulunamadı! Veritabanında bu Guid'e sahip bir kayıt olmayabilir.");
            }
            var model = await _serviceAddress.GetAsync(p => p.AddressGuid.ToString().ToLower() == id.ToLower() && p.AppUserId == appUser.Id);
            if (model != null)
            {
                model.Title = address.Title;
                model.District = address.District;
                model.City = address.City;
                model.OpenAddress = address.OpenAddress;
                model.IsDeliveryAddress = address.IsDeliveryAddress;
                model.IsBillingAddress = address.IsBillingAddress;
                model.IsActive = address.IsActive;
                var otherAddresses = await _serviceAddress.GetAllAsync(p => p.AppUserId == appUser.Id && p.Id != model.Id);
                foreach (var otherAddress in otherAddresses)
                {
                    otherAddress.IsDeliveryAddress = false;
                    otherAddress.IsBillingAddress = false;
                    _serviceAddress.Update(otherAddress);
                }
                try
                {
                    _serviceAddress.Update(model);
                    await _serviceAddress.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Hata Oluştu");
                }

                return View(model);
            }
            else
            {
                return NotFound("Adres bulunamadı! Veritabanında bu Guid'e sahip bir kayıt olmayabilir.");
            }

        }
        public async Task<IActionResult> Delete(string id)

        {
            var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;

            if (string.IsNullOrEmpty(userGuidStr))
            {
                return NotFound("Oturumda UserGuid bulunamadı.");
            }

            // Karşılaştırmayı daha güvenli hale getirelim (büyük/küçük harf duyarlılığını ortadan kaldırarak)
            var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid.ToString().ToLower() == userGuidStr.ToLower());

            if (appUser == null)
            {
                return NotFound("Kullanıcı bulunamadı! Veritabanında bu Guid'e sahip bir kayıt olmayabilir.");
            }
            var model = await _serviceAddress.GetAsync(p => p.AddressGuid.ToString().ToLower() == id.ToLower() && p.AppUserId == appUser.Id);
            if (model != null)
            {
                return View(model);
            }
            else
            {
                return NotFound("Adres bulunamadı! Veritabanında bu Guid'e sahip bir kayıt olmayabilir.");

            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, Address address)

        {
            var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;

            if (string.IsNullOrEmpty(userGuidStr))
            {
                return NotFound("Oturumda UserGuid bulunamadı.");
            }

            // Karşılaştırmayı daha güvenli hale getirelim (büyük/küçük harf duyarlılığını ortadan kaldırarak)
            var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid.ToString().ToLower() == userGuidStr.ToLower());

            if (appUser == null)
            {
                return NotFound("Kullanıcı bulunamadı! Veritabanında bu Guid'e sahip bir kayıt olmayabilir.");
            }
            var model = await _serviceAddress.GetAsync(p => p.AddressGuid.ToString().ToLower() == id.ToLower() && p.AppUserId == appUser.Id);
            if (model != null)
            {
                try
                {
                    _serviceAddress.Delete(model);
                    await _serviceAddress.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch(Exception)
                {
                    ModelState.AddModelError("", "Hata Oluştu");
                }
                return View(model);
            }
            else
            {
                return NotFound("Adres bulunamadı! Veritabanında bu Guid'e sahip bir kayıt olmayabilir.");

            }

        }
    }

}

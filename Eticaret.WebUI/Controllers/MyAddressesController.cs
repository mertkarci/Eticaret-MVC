using System.Threading.Tasks;
using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        [HttpGet("")]
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
        [HttpGet("ekle")]
        public IActionResult Create()
        {
            return View();
        }
        [ValidateAntiForgeryToken]
        [HttpPost("ekle")]
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
        [HttpGet("duzenle/{**name}")]
        public async Task<IActionResult> Edit(string name)
        {
            // KİLİT 1: Eğer URL'de isim yoksa direkt listeye geri yolla (Hata vermesini engeller)
            if (string.IsNullOrEmpty(name)) return RedirectToAction(nameof(Index));

            var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;

            if (string.IsNullOrEmpty(userGuidStr))
            {
                return NotFound("Oturumda UserGuid bulunamadı. Lütfen tekrar giriş yapın.");
            }

            // 2. DÜZELTME: Karşılaştırmayı her iki tarafı da Normalize (küçülterek) ederek yapıyoruz
            // Ayrıca ToString().ToLower() yaparak veritabanı farklarını ortadan kaldırıyoruz.
            var appUser = await _serviceAppUser.GetAsync(p =>
                p.UserGuid.ToString().ToLower() == userGuidStr.ToLower());

            if (appUser == null)
            {
                // Eğer buraya düşüyorsa veritabanında bu Guid'e sahip bir AppUser gerçekten yoktur.
                // Veritabanını açıp AppUser tablosundaki UserGuid sütununu kontrol etmelisin.
                return NotFound($"Kullanıcı bulunamadı! Aranan Guid: {userGuidStr}");
            }

            // DÜZELTME: ToLower() kullanmak yerine direkt eşitliyoruz. SQL bunu sorunsuz anlar.
            var model = await _serviceAddress.GetAsync(p => p.Title == name && p.AppUserId == appUser.Id);

            if (model != null) return View(model);

            return NotFound("Adres bulunamadı!");
        }

        [HttpPost("duzenle/{**name}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string name, [FromForm] string id, Address address)
        {
            // KİLİT 2: Gizli input'tan id gelmemişse işlemi durdur
            if (string.IsNullOrEmpty(id)) return BadRequest("Geçersiz işlem: ID bulunamadı.");
            var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;

            if (string.IsNullOrEmpty(userGuidStr))
            {
                return NotFound("Oturumda UserGuid bulunamadı. Lütfen tekrar giriş yapın.");
            }

            // 2. DÜZELTME: Karşılaştırmayı her iki tarafı da Normalize (küçülterek) ederek yapıyoruz
            // Ayrıca ToString().ToLower() yaparak veritabanı farklarını ortadan kaldırıyoruz.
            var appUser = await _serviceAppUser.GetAsync(p =>
                p.UserGuid.ToString().ToLower() == userGuidStr.ToLower());

            if (appUser == null)
            {
                // Eğer buraya düşüyorsa veritabanında bu Guid'e sahip bir AppUser gerçekten yoktur.
                // Veritabanını açıp AppUser tablosundaki UserGuid sütununu kontrol etmelisin.
                return NotFound($"Kullanıcı bulunamadı! Aranan Guid: {userGuidStr}");
            }

            // PROFESYONEL DOKUNUŞ: String olarak gelen id'yi gerçek bir Guid nesnesine çeviriyoruz
            if (!Guid.TryParse(id, out Guid parsedGuid))
            {
                return BadRequest("Bozuk veya geçersiz bir ID gönderildi.");
            }

            // ARTIK HATA VEREMEZ: Çünkü ToLower() veya ToString() yok, iki temiz Guid'i karşılaştırıyoruz
            var model = await _serviceAddress.GetAsync(p => p.AddressGuid == parsedGuid && p.AppUserId == appUser.Id);

            if (model != null)
            {
                model.Title = address.Title;
                model.District = address.District;
                model.City = address.City;
                model.OpenAddress = address.OpenAddress;
                model.IsDeliveryAddress = address.IsDeliveryAddress;
                model.IsBillingAddress = address.IsBillingAddress;
                model.IsActive = address.IsActive;

                if (model.IsDeliveryAddress || model.IsBillingAddress)
                {
                    var otherAddresses = await _serviceAddress.GetAllAsync(p => p.AppUserId == appUser.Id && p.Id != model.Id);
                    foreach (var otherAddress in otherAddresses)
                    {
                        if (model.IsDeliveryAddress) otherAddress.IsDeliveryAddress = false;
                        if (model.IsBillingAddress) otherAddress.IsBillingAddress = false;
                        _serviceAddress.Update(otherAddress);
                    }
                }

                try
                {
                    _serviceAddress.Update(model);
                    await _serviceAddress.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Kayıt sırasında bir hata oluştu.");
                }
                return View(model);
            }

            return NotFound("Adres bulunamadı!");
        }
        [HttpGet("sil/{**name}")]
        public async Task<IActionResult> Delete(string name)
        {
            var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;

            if (string.IsNullOrEmpty(userGuidStr))
            {
                return NotFound("Oturumda UserGuid bulunamadı.");
            }

            var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid.ToString().ToLower() == userGuidStr.ToLower());

            if (appUser == null)
            {
                return NotFound("Kullanıcı bulunamadı! Veritabanında bu Guid'e sahip bir kayıt olmayabilir.");
            }

            // DÜZELTİLEN KISIM BURASI: id (Guid) yerine name (Title) üzerinden arıyoruz
            var model = await _serviceAddress.GetAsync(p => p.Title.ToLower() == name.ToLower() && p.AppUserId == appUser.Id);

            if (model != null)
            {
                return View(model);
            }
            else
            {
                return NotFound("Adres bulunamadı! Veritabanında bu isme sahip bir kayıt olmayabilir.");
            }
        }

        [HttpPost("sil/{name}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string name, [FromForm] string id)
        {
            var userGuidStr = HttpContext.User.FindFirst("UserGuid")?.Value;

            if (string.IsNullOrEmpty(userGuidStr)) return NotFound("Oturumda UserGuid bulunamadı.");

            var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid.ToString().ToLower() == userGuidStr.ToLower());

            if (appUser == null) return NotFound("Kullanıcı bulunamadı!");


            if (string.IsNullOrEmpty(id)) return BadRequest("Silinecek adresin kimliği (ID) bulunamadı.");


            var model = await _serviceAddress.GetAsync(p => p.AddressGuid.ToString().ToLower() == id.ToLower() && p.AppUserId == appUser.Id);

            if (model != null)
            {
                try
                {
                    _serviceAddress.Delete(model);
                    await _serviceAddress.SaveChangesAsync();
                    return RedirectToAction(nameof(Index)); // Silince listeye dön
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Silme işlemi sırasında bir hata oluştu.");
                }
                return View(model); // Hata varsa aynı sayfada kal
            }
            else
            {
                return NotFound("Adres bulunamadı veya silmeye yetkiniz yok.");
            }
        }
    }

}

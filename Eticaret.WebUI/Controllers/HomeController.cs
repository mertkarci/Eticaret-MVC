using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Eticaret.WebUI.Models;
using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;

namespace Eticaret.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly IService<Product> _serviceProduct;
    private readonly IService<Slider> _serviceSlider;
    private readonly IService<News> _serviceNews;
        private readonly IService<Contact> _serviceContact;

    public HomeController(IService<Product> serviceProduct,IService<Slider> serviceSlider,IService<News> serviceNews, IService<Contact> serviceContact)
    {
        _serviceProduct = serviceProduct;
        _serviceSlider = serviceSlider;
        _serviceNews = serviceNews;
        _serviceContact = serviceContact;

    }
    public async Task<IActionResult> Index()
    {
        var model = new HomePageViewModel()
        {
            Sliders = await _serviceSlider.GetAllAsync(),
            Products = await _serviceProduct.GetAllAsync(p => p.isActive && p.isHome),
            News = await _serviceNews.GetAllAsync()
            
        };


        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }
    [Route("AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View();
    }
    // GET: Sayfayı Gösterir
    public IActionResult ContactUs()
    {
        return View();
    }

    // POST: Formu Karşılar
    [HttpPost]
    [ValidateAntiForgeryToken] // Güvenlik önlemi (CSRF saldırılarına karşı)
    public async Task<IActionResult> ContactUs(ContactViewModel model)
    {
        // 1. Model geçerli mi? (Boş alan var mı?)
        if (ModelState.IsValid)
        {
            try
            {
                // 2. ViewModel'i -> Gerçek Entity'e Dönüştürme (Mapping)
                // Kullanıcı sadece ad, mail, konu ve mesaj gönderdi.
                // Tarih gibi sistem verilerini biz burada atıyoruz.
                var contact = new Contact()
                {
                    Name = model.Name,
                    Email = model.Email,
                    Surname = model.Surname,
                    Phone = model.Phone,
                    Message = model.Message,
                    CreateDate = DateTime.Now // Veritabanında tarih alanı varsa
                };

                // 3. Veritabanına Ekleme
                await _serviceContact.AddAsync(contact);
                var result = await _serviceContact.SaveChangesAsync();

                if (result > 0)
                {
                    TempData["Message"] = "Mesajınız başarıyla gönderildi.";

                    return RedirectToAction("ContactUs");
                }
            }
            catch (Exception)
            {
                // Hata olursa kullanıcıya genel bir hata mesajı
                ModelState.AddModelError("", "Bir hata oluştu, lütfen tekrar deneyin.");
            }
        }

        // Hata varsa veya model geçersizse formu (yazılanlarla birlikte) geri gönder
        return View(model);
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

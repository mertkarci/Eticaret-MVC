using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Eticaret.WebUI.Models;
using Eticaret.Data;
using Microsoft.EntityFrameworkCore;
using Eticaret.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Threading.Tasks;
using Eticaret.WebUI.Utils;

namespace Eticaret.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly DatabaseContext _context;

    public HomeController(DatabaseContext context)
    {
        _context = context;
    }
    public async Task<IActionResult> Index()
    {
        var model = new HomePageViewModel()
        {
            Sliders = await _context.Sliders.ToListAsync(),
            Products = await _context.Products.Where(p => p.isActive && p.isHome).ToListAsync(),
            News = await _context.News.ToListAsync()
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
                await _context.Contacts.AddAsync(contact);
                var result = await _context.SaveChangesAsync();

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

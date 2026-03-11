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
    [Route("iletisim")]
    public IActionResult ContactUs()
    {
        return View();
    }


    [HttpPost, Route("iletisim")]
    [ValidateAntiForgeryToken] 
    public async Task<IActionResult> ContactUs(ContactViewModel model)
    {
 
        if (ModelState.IsValid)
        {
            try
            {

                var contact = new Contact()
                {
                    Name = model.Name,
                    Email = model.Email,
                    Surname = model.Surname,
                    Phone = model.Phone,
                    Message = model.Message,
                    CreateDate = DateTime.Now 
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
                ModelState.AddModelError("", "Bir hata oluştu, lütfen tekrar deneyin.");
            }
        }

        return View(model);
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

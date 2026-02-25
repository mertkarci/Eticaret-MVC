// Dosya Yolu: /Users/mesut/Desktop/Eticaret/Eticaret.WebUI/ViewComponents/ThemeViewComponent.cs
// (Bu yeni bir dosyadır, lütfen oluşturun)

using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Eticaret.WebUI.ViewComponents
{
    public class ThemeViewComponent : ViewComponent
    {
        private readonly IService<ThemeSetting> _service;

        public ThemeViewComponent(IService<ThemeSetting> service)
        {
            _service = service;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Aktif olan temayı getir
            var theme = await _service.GetAsync(x => x.IsActive);

            // Eğer veritabanında tema yoksa varsayılan değerlerle bir tane oluştur (Hata almamak için)
            if (theme == null)
            {
                theme = new ThemeSetting
                {
                    MainColor = "#0d6efd",
                    SecondaryColor = "#6c757d",
                    BackgroundColor = "#f8f9fa",
                    TextColor = "#212529",
                    NavbarBgColor = "#ffffff",
                    FooterBgColor = "#343a40"
                };
            }

            return View(theme);
        }
    }
}

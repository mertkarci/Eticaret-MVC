using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Eticaret.WebUI.ViewComponents
{
    public class PopupNotificationViewComponent : ViewComponent
    {
        private readonly IService<Notification> _serviceNotification;

        public PopupNotificationViewComponent(IService<Notification> serviceNotification)
        {
            _serviceNotification = serviceNotification;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Sadece isActive olan İLK (veya en son eklenen) bildirimi getir
            var activeNotification = await _serviceNotification.GetAsync(n => n.isActive == true);

            // Eğer yayında bildirim yoksa boş dön (HTML üretilmez)
            if (activeNotification == null)
            {
                return Content(string.Empty); 
            }

            return View(activeNotification);
        }
    }
}
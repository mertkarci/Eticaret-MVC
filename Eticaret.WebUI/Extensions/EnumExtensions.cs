using Eticaret.Core.Entities; // Kendi OrderState enum'ının olduğu namespace
using System;

namespace Eticaret.WebUI.Extensions
{
    public static class OrderStateExtensions
    {
        // Bu metot geriye 2 string dönen bir "Tuple" (Paket) yapısı döndürür:
        // (BadgeClass, StatusIcon)
        public static (string BadgeClass, string Icon) GetStatusUI(this Enum orderState)
        {
            var statusStr = orderState.ToString();

            return statusStr switch
            {
                "Waiting" => ("bg-warning bg-opacity-90 mytext border-warning", "hourglass-split"),
                "Shipped" => ("bg-info bg-opacity-90 mytext border-info", "truck"),
                
                // DÜZELTİLEN KISIM: Özel CSS yerine Bootstrap 5 Açık Yeşil (Opacity 50)
                "Approved" => ("bg-success bg-opacity-70 mytext border-success", "box2-fill"), 
                
                "Completed" => ("bg-success bg-opacity-20 mytext border-success", "check-circle-fill"),
                "Rejected" => ("bg-danger bg-opacity-20 mytext border-danger", "x-circle-fill"),
                
                // Eğer tanımlı olmayan bir durum gelirse varsayılan değer:
                _ => ("bg-secondary bg-opacity-10 text-secondary border-secondary", "circle")
            };
        }
    }
}
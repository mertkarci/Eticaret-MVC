using Eticaret.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Eticaret.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class MainController : Controller
    {
        private readonly DatabaseContext _context;

        public MainController(DatabaseContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalSales = await _context.Orders.SumAsync(o => (decimal?)o.TotalPrice) ?? 0;
            ViewBag.TotalOrders = await _context.Orders.CountAsync();

            // YENİ KARTLAR: Sipariş durumlarının isimlerine göre dinamik olarak sayıyoruz
            // (Enum yapınızın adı tam bilinmediği için ToString() ile en güvenli kelime aramasını yapıyoruz)
            var allStates = await _context.Orders.Select(o => o.OrderState).ToListAsync();
            ViewBag.PendingOrders = allStates.Count(s => s.ToString().IndexOf("Bekliyor", StringComparison.OrdinalIgnoreCase) >= 0 || s.ToString().IndexOf("Pending", StringComparison.OrdinalIgnoreCase) >= 0);
            ViewBag.CancelledOrders = allStates.Count(s => s.ToString().IndexOf("Iptal", StringComparison.OrdinalIgnoreCase) >= 0 || s.ToString().IndexOf("Cancel", StringComparison.OrdinalIgnoreCase) >= 0 || s.ToString() == "İptal Edildi");

            // 2. ÇİZGİ GRAFİK (Son 6 Ayın Satışları)
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .OrderBy(m => m)
                .ToList();

            var areaLabels = new List<string>();
            var areaData = new List<decimal>();

            foreach (var month in last6Months)
            {
                areaLabels.Add(month.ToString("MMM yyyy", new CultureInfo("tr-TR"))); // Eki 2023
                var monthlyTotal = await _context.Orders
                    .Where(o => o.OrderDate.Year == month.Year && o.OrderDate.Month == month.Month)
                    .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;
                areaData.Add(monthlyTotal);
            }

            ViewBag.AreaChartLabels = areaLabels;
            ViewBag.AreaChartData = areaData;

            // 3. SÜTUN GRAFİK (Sipariş Durumlarına Göre Dağılım)
            var orderStates = await _context.Orders
                .GroupBy(o => o.OrderState)
                .Select(g => new { State = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            ViewBag.BarChartLabels = orderStates.Select(o => o.State).ToList();
            ViewBag.BarChartData = orderStates.Select(o => o.Count).ToList();

            // 4. YENİ GRAFİK: BUGÜNÜN SAATLİK SİPARİŞLERİ
            var today = DateTime.Today;
            var todaysOrders = await _context.Orders
                .Where(o => o.OrderDate >= today)
                .ToListAsync();

            ViewBag.TodayChartLabels = Enumerable.Range(0, 24).Select(h => $"{h:D2}:00").ToList();
            ViewBag.TodayChartData = Enumerable.Range(0, 24).Select(h => 
                todaysOrders.Where(o => o.OrderDate.Hour == h).Sum(o => (decimal?)o.TotalPrice) ?? 0
            ).ToList();

            // ALT TABLO: Son Eklenen 10 Ürün (Tablo şişmesin diye sınırlandırdık)
            ViewBag.Products = await _context.Products.OrderByDescending(p => p.Id).Take(10).ToListAsync();
            
            return View();
        }
    }
}

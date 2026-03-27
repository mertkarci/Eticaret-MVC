using Eticaret.Core.Entities;
using Eticaret.WebUI.Models;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.WebUI.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly IService<Order> _orderService;
        private readonly IService<Comment> _commentService;
        private readonly IService<OrderLine> _orderLineService;

        public ReviewsController(IService<Order> orderService, IService<Comment> commentService, IService<OrderLine> orderLineService)
        {
            _orderService = orderService;
            _commentService = commentService;
            _orderLineService = orderLineService;
        }

        [HttpGet]
        public async Task<IActionResult> Order(Guid token)
        {
            if (token == Guid.Empty)
            {
                return BadRequest("Geçersiz bir bağlantı kullandınız. Siparişinizin değerlendirme kodu (ReviewToken) boş (Guid.Empty) görünüyor. Lütfen veritabanından siparişinize geçerli bir Token atayın veya yeni bir sipariş ile test edin.");
            }

            // 1. Find the order by the secure token
            var order = await _orderService.GetQueryable()
                .Include(o => o.OrderLines)
                    .ThenInclude(ol => ol.Product)
                .FirstOrDefaultAsync(o => o.ReviewToken == token);

            if (order == null) return NotFound("Sipariş bulunamadı veya geçersiz bir bağlantı kullandınız.");

            // 2. Check if order is eligible for reviews (Must be Completed)
            if (order.OrderState != EnumOrderState.Completed)
            {
                return BadRequest($"Bu sipariş henüz tamamlanmadığı için değerlendirme yapılamaz. (Siparişin Mevcut Durumu: {order.OrderState})");
            }

            // 3. Get existing comments made by THIS USER to filter them out (Kullanıcı aynı ürünü daha önce değerlendirdiyse form çıkmasın)
            List<int?> reviewedProductIds;
            if (order.AppUserId.HasValue)
            {
                var userComments = await _commentService.GetAllAsync(c => c.AppUserId == order.AppUserId.Value);
                reviewedProductIds = userComments.Select(c => c.ProductId).ToList();
            }
            else
            {
                var guestComments = await _commentService.GetAllAsync(c => c.GuestEmail == order.CustomerEmail);
                reviewedProductIds = guestComments.Select(c => c.ProductId).ToList();
            }

            // 4. Get unique products from the order, EXCLUDING already reviewed ones
            var pendingProducts = order.OrderLines
                .Where(ol => !reviewedProductIds.Contains(ol.ProductId))
                .DistinctBy(ol => ol.ProductId) // Prevents multiple forms if they bought 4 of the same item
                .Select(ol => new ReviewableProductViewModel
                {
                    ProductId = ol.ProductId,
                    ProductName = ol.Product?.Name,
                    ProductImage = ol.Product?.Image,
                    UnitPrice = ol.UnitPrice
                }).ToList();

            var viewModel = new OrderReviewViewModel
            {
                OrderNumber = order.OrderNumber,
                ReviewToken = token,
                PendingReviews = pendingProducts
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(Guid token, int productId, int rating, string description)
        {
            if (token == Guid.Empty)
            {
                TempData["Error"] = "Geçersiz sipariş anahtarı.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _orderService.GetAsync(o => o.ReviewToken == token);
            if (order == null || order.OrderState != EnumOrderState.Completed)
            {
                TempData["Error"] = "Geçersiz işlem veya sipariş bulunamadı.";
                return RedirectToAction("Index", "Home"); // Güvenli bir yere yolla
            }

            Comment? existingComment;
            if (order.AppUserId.HasValue)
                existingComment = await _commentService.GetAsync(c => c.AppUserId == order.AppUserId.Value && c.ProductId == productId);
            else
                existingComment = await _commentService.GetAsync(c => c.GuestEmail == order.CustomerEmail && c.ProductId == productId);

            if (existingComment != null)
            {
                TempData["Error"] = "Bu ürünü daha önce değerlendirdiniz. Bir ürüne sadece bir kez yorum yapabilirsiniz.";
                return RedirectToAction("Order", new { token = token });
            }

            var lineItem = await _orderLineService.GetAsync(ol => ol.OrderId == order.Id && ol.ProductId == productId);
            if (lineItem == null)
            {
                TempData["Error"] = "Bu ürün siparişte bulunamadı.";
                return RedirectToAction("Order", new { token = token });
            }

            var comment = new Comment
            {
                OrderId = order.Id,
                ProductId = productId,
                AppUserId = order.AppUserId, // Null if guest
                GuestName = order.AppUserId == null ? order.CustomerName : null, // Only set if it's a guest order
                GuestEmail = order.AppUserId == null ? order.CustomerEmail : null, // Only set if it's a guest order
                Rating = rating,
                Description = description,
                IsActive = true
            };

            await _commentService.AddAsync(comment);
            await _commentService.SaveChangesAsync();

            TempData["Success"] = "Değerlendirmeniz alınmıştır, teşekkür ederiz!";
            return RedirectToAction("Order", new { token = token });
        }
    }
}
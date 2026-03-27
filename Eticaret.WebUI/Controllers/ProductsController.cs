using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Eticaret.WebUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.WebUI.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IService<Product> _productService;
        private readonly IService<Comment> _commentService;

        public ProductsController(IService<Product> productService, IService<Comment> commentService)
        {
            _productService = productService;
            _commentService = commentService;
        }

        [Route("/urun/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var product = await _productService.GetQueryable()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (product == null)
            {
                return NotFound();
            }

            // Fetch related products (e.g., from the same category, excluding the current one)
            var relatedProducts = await _productService.GetQueryable()
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.isActive)
                .Take(5) // Limit to 5 related products
                .ToListAsync();

            var productDetailViewModel = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            // Yorumları Optimize Ederek Çek (Sadece istatistikleri ve ilk 2 yorumu al)
            var reviewQuery = _commentService.GetQueryable()
                .Where(c => c.ProductId == product.Id && c.IsActive);

            var totalReviews = await reviewQuery.CountAsync();
            var reviewViewModel = new ProductReviewViewModel
            {
                ProductId = product.Id,
                TotalReviews = totalReviews,
                Comments = new List<Comment>()
            };

            if (totalReviews > 0)
            {
                reviewViewModel.AverageRating = await reviewQuery.AverageAsync(c => c.Rating ?? 0);
                
                var groupedRatings = await reviewQuery
                    .GroupBy(c => c.Rating ?? 0)
                    .Select(g => new { Rating = g.Key, Count = g.Count() })
                    .ToListAsync();
                    
                reviewViewModel.RatingCounts = groupedRatings.ToDictionary(g => g.Rating, g => g.Count);

                // Sadece ilk 2 yorumu RAM'e al
                reviewViewModel.Comments = await reviewQuery
                    .Include(c => c.AppUser)
                    .OrderByDescending(c => c.CreateDate)
                    .Take(2)
                    .ToListAsync();
            }

            ViewBag.Reviews = reviewViewModel;

            return View(productDetailViewModel);
        }

        // "Daha Fazla Yorum İncele" butonunun 5'er 5'er veri çekeceği Endpoint
        [HttpGet("/urun/yorumlar")]
        public async Task<IActionResult> LoadMoreReviews(int productId, int skip = 2, int take = 5)
        {
            var comments = await _commentService.GetQueryable()
                .Include(c => c.AppUser)
                .Where(c => c.ProductId == productId && c.IsActive)
                .OrderByDescending(c => c.CreateDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var result = comments.Select(c => {
                var displayName = !string.IsNullOrEmpty(c.AppUser?.Name) 
                    ? $"{c.AppUser.Name} {c.AppUser.Surname?.Substring(0,1)}." 
                    : (!string.IsNullOrEmpty(c.GuestName) ? c.GuestName : "Gizli Müşteri");
                
                var timeSpan = DateTime.Now - c.CreateDate;
                string timeAgo = "Az önce";
                if (timeSpan.TotalDays > 365) timeAgo = $"{(int)(timeSpan.TotalDays / 365)} yıl önce";
                else if (timeSpan.TotalDays > 30) timeAgo = $"{(int)(timeSpan.TotalDays / 30)} ay önce";
                else if (timeSpan.TotalDays >= 1) timeAgo = $"{(int)timeSpan.TotalDays} gün önce";
                else if (timeSpan.TotalHours >= 1) timeAgo = $"{(int)timeSpan.TotalHours} saat önce";
                else if (timeSpan.TotalMinutes >= 1) timeAgo = $"{(int)timeSpan.TotalMinutes} dakika önce";

                return new {
                    displayName = displayName,
                    initial = !string.IsNullOrEmpty(displayName) ? displayName.Substring(0, 1).ToUpper() : "?",
                    dateText = timeAgo,
                    rating = c.Rating ?? 0,
                    description = c.Description
                };
            });

            return Json(result);
        }
    }
}
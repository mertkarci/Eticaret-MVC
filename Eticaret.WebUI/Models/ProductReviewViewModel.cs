using Eticaret.Core.Entities;

namespace Eticaret.WebUI.Models
{
    public class ProductReviewViewModel
    {
        public int ProductId { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingCounts { get; set; } = new Dictionary<int, int>();
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
}
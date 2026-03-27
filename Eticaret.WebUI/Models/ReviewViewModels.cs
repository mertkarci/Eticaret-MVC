namespace Eticaret.WebUI.Models
{
    public class OrderReviewViewModel
    {
        public string OrderNumber { get; set; }
        public Guid ReviewToken { get; set; }
        
        // List of products they haven't reviewed yet
        public List<ReviewableProductViewModel> PendingReviews { get; set; } = new();
    }

    public class ReviewableProductViewModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImage { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
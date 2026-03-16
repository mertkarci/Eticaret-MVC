using Eticaret.Core.Entities;

namespace Eticaret.WebUI.Models
{
    public class CategoryFilterViewModel
    {

        public Category CurrentCategory { get; set; }

        public List<Category> SubCategories { get; set; } = new List<Category>();

        public List<Brand> AvailableBrands { get; set; } = new List<Brand>();

        public List<int> SelectedSubCategoryIds { get; set; } = new List<int>();
        public List<int> SelectedBrands { get; set; } = new List<int>();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SearchText { get; set; }

        public List<Product> FilteredProducts { get; set; } = new List<Product>();
    }
}
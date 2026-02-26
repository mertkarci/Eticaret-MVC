using Eticaret.Core.Entities;

namespace Eticaret.WebUI.Models
{
    public class CategoryFilterViewModel
    {
        // Hangi kategorinin sayfasındayız? (Başlık vs. için)
        public Category CurrentCategory { get; set; }

        // --- EKRANA ÇİZİLECEK SEÇENEKLER ---
        public List<Category> SubCategories { get; set; } = new List<Category>();
        // Eskiden List<string> idi, artık List<Brand> oldu
        public List<Brand> AvailableBrands { get; set; } = new List<Brand>();

        // Kullanıcının formdan seçeceği marka ID'leri (Eskiden List<string> idi)


        // --- KULLANICININ SEÇTİĞİ FİLTRELER (Formdan Gelecek) ---
        public List<int> SelectedSubCategoryIds { get; set; } = new List<int>();
        public List<int> SelectedBrands { get; set; } = new List<int>();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SearchText { get; set; }

        // --- FİLTRELENMİŞ SONUÇ ---
        public List<Product> FilteredProducts { get; set; } = new List<Product>();
    }
}
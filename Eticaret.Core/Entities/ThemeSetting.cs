

using System.ComponentModel.DataAnnotations;

namespace Eticaret.Core.Entities
{
    public class ThemeSetting : IEntity
    {
        public int Id { get; set; }
        
        [Display(Name = "Tema Adı")]
        public string Name { get; set; } = "Varsayılan Tema";

        [Display(Name = "Ana Renk (Primary)")]
        public string MainColor { get; set; } = "#0d6efd"; // Bootstrap Default Blue

        [Display(Name = "İkincil Renk (Secondary)")]
        public string SecondaryColor { get; set; } = "#6c757d"; // Bootstrap Gray

        [Display(Name = "Arka Plan Rengi")]
        public string BackgroundColor { get; set; } = "#f8f9fa"; // Light Gray

        [Display(Name = "Yazı Rengi")]
        public string TextColor { get; set; } = "#212529"; // Dark Gray

        [Display(Name = "Navbar Arkaplanı")]
        public string NavbarBgColor { get; set; } = "#ffffff";

        [Display(Name = "Footer Arkaplanı")]
        public string FooterBgColor { get; set; } = "#343a40";

        [Display(Name = "Aktif Tema mı?")]
        public bool IsActive { get; set; }
    }
}

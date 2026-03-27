using System.ComponentModel.DataAnnotations;

namespace Eticaret.Core.Entities
{
    public class Comment : IEntity
    {
        public int Id { get; set; }
 
        [Display(Name = "Müşteri ID")]
        public int? AppUserId { get; set; }
 
        [Display(Name = "Müşteri")]
        public AppUser? AppUser { get; set; }
 
        [Display(Name = "Ürün")]
        public Product? Product { get; set; }
 
        [Display(Name = "Ürün ID")]
        public int? ProductId { get; set; }
 
        [Display(Name = "Sipariş ID")]
        public int? OrderId { get; set; }

        [Display(Name = "Yorum"), StringLength(500), DataType(DataType.MultilineText), Required(ErrorMessage = "{0} Alanı Zorunludur!")]
        public string Description { get; set; }
 
        [Display(Name = "Aktif")]
        public bool IsActive { get; set; }
 
        [Display(Name = "Kayıt Tarihi"), ScaffoldColumn(false)]
        public DateTime CreateDate { get; set; } = DateTime.Now;
 
        // Guest user info
        [Display(Name = "Ad Soyad"), StringLength(50)]
        public string? GuestName { get; set; }
 
        [Display(Name = "E-posta"), StringLength(50), EmailAddress]
        public string? GuestEmail { get; set; }
 
        [Display(Name = "Puan (1-5)")]
        [Range(1, 5)]
        public int? Rating { get; set; }
 
        // For threaded comments/replies
        public int? ParentId { get; set; }
        public Comment? Parent { get; set; }
        public ICollection<Comment>? Replies { get; set; }

    }
}
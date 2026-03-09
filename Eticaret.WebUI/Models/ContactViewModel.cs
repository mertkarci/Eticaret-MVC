using System.ComponentModel.DataAnnotations;

namespace Eticaret.WebUI.Models;

public class ContactViewModel
{
    [Display(Name = "Ad")]
    [Required(ErrorMessage = "Ad alanı boş geçilemez!")]
    public string Name { get; set; }

    [Required(ErrorMessage = "E-posta alanı boş geçilemez!")]
    [EmailAddress(ErrorMessage = "Geçerli bir mail giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; }
    [Required(ErrorMessage = "Soyad alanı boş geçilemez!")]

    [Display(Name = "Soyad")]
    public string Surname { get; set; }

    [Required(ErrorMessage = "Telefon alanı boş geçilemez!")]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Lütfen bir mesaj yazın.")]
    [MinLength(20, ErrorMessage = "Mesajınız çok kısa! Lütfen en az 20 karakterlik bir açıklama yapın.")]
    [Display(Name = "Mesaj")]
    public string Message { get; set; }

    [Display(Name = "Kayıt Tarihi"), ScaffoldColumn(false)]
    public DateTime CreateDate { get; set; } = DateTime.Now;
}
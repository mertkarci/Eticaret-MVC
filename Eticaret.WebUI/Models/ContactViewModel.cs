using System.ComponentModel.DataAnnotations;

namespace Eticaret.WebUI.Models;

public class ContactViewModel
{
    [Display(Name ="Ad")]
    [Required(ErrorMessage = "Ad alanı boş geçilemez!")]
    public string Name { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir mail giriniz.")]
    public string Email { get; set; }

    [Display(Name ="Soyad")]
    public string Surname {get; set;}


    [Display(Name ="Telefon")]
    public string? Phone {get; set;}

    [Display(Name ="Mesaj")]
    public string Message {get; set;}

    [Display(Name ="Kayıt Tarihi"), ScaffoldColumn(false)]
    public DateTime CreateDate {get; set;}  = DateTime.Now;
}
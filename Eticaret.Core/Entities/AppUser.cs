using System.ComponentModel.DataAnnotations;

namespace Eticaret.Core.Entities;

public class AppUser : IEntity
{
    public int Id {get; set;}

    [Display(Name ="Adı")]
    public string Name {get; set;}

    [Display(Name ="Soyadı")]
    public string Surname {get; set;}

    [Display(Name ="Eposta")]
    public string Email {get; set;}

    [Display(Name ="Telefon")]
    public string Phone {get; set;}

    [Display(Name ="Şifre")]
    public string Password {get; set;}

    [Display(Name ="Kullanıcı Adı")]
    public string? Username {get; set;}
    public bool isActive {get; set;}
    public bool isAdmin{get; set;}

    [Display(Name ="Kayıt Tarihi"), ScaffoldColumn(false)]
    public DateTime CreateDate {get; set;} = DateTime.Now;
    public Guid? UserGuid {get; set;} = Guid.NewGuid(); 
}

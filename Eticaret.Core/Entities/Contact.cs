using System.ComponentModel.DataAnnotations;

namespace Eticaret.Core.Entities;

public class Contact : IEntity
{
    public int Id { get; set;}

    [Display(Name ="Ad")]
    public string Name {get; set;}

    [Display(Name ="Soyad")]
    public string Surname {get; set;}

    [Display(Name ="Eposta")]
    public string? Email {get; set;}

    [Display(Name ="Telefon")]
    public string? Phone {get; set;}

    [Display(Name ="Mesaj")]
    public string Message {get; set;}

    [Display(Name ="Kayıt Tarihi"), ScaffoldColumn(false)]
    public DateTime CreateDate {get; set;}  = DateTime.Now;
}

using System.ComponentModel.DataAnnotations;
using Eticaret.Core.Entities;

namespace Eticaret.Core.Entities;

public class Brand : IEntity
{
    public int Id {get; set;}

    [Display(Name ="Açıklama")]
    public string? Description {get; set;}

    [Display(Name ="Marka Adı")]
    public string Name {get; set;}

    [Display(Name ="Marka Logosu")]
    public string? Logo {get; set;}
    public bool isActive {get; set;}

    [Display(Name ="Kayıt Tarihi"), ScaffoldColumn(false)]
    public DateTime CreateDate {get; set;} = DateTime.Now;
    public IList<Product>? Products {get; set;}
}

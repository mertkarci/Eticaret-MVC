using System.ComponentModel.DataAnnotations;
using Eticaret.Core.Entities;

namespace Eticaret.Core.Entities;

public class Category : IEntity
{
    public int Id {get; set;}
    public int ParentId {get; set;}

    [Display(Name ="Sıra Numarası")]
    public int OrderNo {get; set;}

    [Display(Name ="Açıklama")]
    public string? Description {get; set;}

    [Display(Name ="Kategori Adı")]
    public string? Name {get; set;}

    [Display(Name ="Görsel")]
    public string? Image {get; set;}
    public bool isActive {get; set;}
    public bool isTopMenu {get; set;}

    [Display(Name ="Kayıt Tarihi"), ScaffoldColumn(false)]
    public DateTime CreateDate {get; set;} = DateTime.Now;
    public IList<Product>? Products {get; set;}
}

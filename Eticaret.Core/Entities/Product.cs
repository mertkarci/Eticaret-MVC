using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Eticaret.Core.Entities;

public class Product : IEntity
{
    public int Id {get; set;}
    public int ParentId {get; set;}

    [Display(Name ="Sıra Numarası")]
    public int OrderNo {get; set;}

    [Display(Name ="Stok Sayısı")]
    public int Stock {get; set;}

    [Display(Name ="Kategori ID")]
    public int? CategoryId {get; set;}
    public Category? Category {get; set;} 

    [Display(Name ="Marka ID")]
    public int BrandId {get; set;}
    public Brand? Brand {get; set;}

    [Display(Name ="Fiyat")]
    public decimal Price {get; set;}

    [Display(Name ="Ürün Numarası")]
    public string? ProductCode {get; set;}

    [Display(Name ="Açıklama")]
    public string? Description {get; set;}

    [Display(Name ="Ürün Adı")]
    public string?Name {get; set;}

    [Display(Name ="Ürün Görseli")]
    public string? Image {get; set;}
    public bool isActive {get; set;}
    public bool isHome {get; set;}

    [Display(Name ="Kayıt Tarihi"), ScaffoldColumn(false)]
    public DateTime CreateDate {get; set;}
}

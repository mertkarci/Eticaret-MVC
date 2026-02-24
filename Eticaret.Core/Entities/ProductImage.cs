using System.ComponentModel.DataAnnotations;
using Eticaret.Core.Entities;

namespace Eticaret.Core.Entities;

public class ProductImage : IEntity
{
    public int Id {get;set;}
    [Display(Name ="Görsel Adı"), StringLength(240)]
    public string? Name {get; set;}
        [Display(Name ="Görsel Açıklama"), StringLength(240)]
    public string? Alt {get; set;}

    [Display(Name ="Ürün")]
    public int? ProductId {get; set;}
    public Product? Product {get; set;}

}

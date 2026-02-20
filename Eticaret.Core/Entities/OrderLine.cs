using System.ComponentModel.DataAnnotations;
using Eticaret.Core.Entities;

namespace Eticaret.Core.Entities;

public class OrderLine : IEntity

{
    public int Id { get; set; }
    [Display(Name = "Sipariş ID")]
    public int OrderId { get; set; }
    [Display(Name = "Ürün ID")]
    public int ProductId { get; set; }
    [Display(Name = "Adet")]
    public int Quantity { get; set; }
    [Display(Name = "Birim Fiyatı")]
    public decimal UnitPrice { get; set; }
    [Display(Name = "Sipariş")]
    public Order? Order { get; set; }
    [Display(Name = "Ürün")]
    public Product? Product { get; set; }
    
}

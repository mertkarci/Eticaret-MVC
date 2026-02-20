using System.Transactions;
using System.ComponentModel.DataAnnotations;
namespace Eticaret.Core.Entities;

public class Order : IEntity
{
    public int Id { get; set; }
    [Display(Name = "Sipariş Numarası"),StringLength(50)]
    public string OrderNumber{ get; set; }
    [Display(Name = "Toplam Tutar")]
    public decimal TotalPrice{ get; set; }
    [Display(Name = "Sipariş Tarihi")]
    public DateTime OrderDate{ get; set; }
    [Display(Name = "Müşteri No")]
    public int AppUserId { get; set; }
    [Display(Name = "Müşteri"),StringLength(50)]
    public string CustomerId { get; set; }
    [Display(Name = "Fatura Adresi"),StringLength(50)]
    public string BillingAddress { get; set; }
    [Display(Name = "Teslimat Adresi"),StringLength(50)]
    public string DeliveryAddress { get; set; }

    public List<OrderLine>? OrderLines { get; set; }
}

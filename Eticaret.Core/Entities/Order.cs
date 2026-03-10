using System.Transactions;
using System.ComponentModel.DataAnnotations;
namespace Eticaret.Core.Entities;

public class Order : IEntity
{
    public int Id { get; set; }

    [Display(Name = "Sipariş No"), StringLength(50)]
    public string OrderNumber { get; set; }

    [Display(Name = "Toplam Tutar")]
    public decimal TotalPrice { get; set; }

    [Display(Name = "Sipariş Tarihi")]
    public DateTime OrderDate { get; set; }

    [Display(Name = "Müşteri No")]
    public int? AppUserId { get; set; }

    [Display(Name = "Ad Soyad"), StringLength(100)]
    public string CustomerName { get; set; }
    [Display(Name = "Müşteri"), StringLength(50)]

    public string? CustomerId { get; set; }

    [Display(Name = "E-Posta"), StringLength(100)]
    public string CustomerEmail { get; set; }

    [Display(Name = "Telefon"), StringLength(20)]
    public string? CustomerPhone { get; set; }

    [Display(Name = "Fatura Adresi"), StringLength(500)]
    public string BillingAddress { get; set; }

    [Display(Name = "Teslimat Adresi"), StringLength(500)]
    public string DeliveryAddress { get; set; }

    public List<OrderLine>? OrderLines { get; set; }

    public AppUser? AppUser { get; set; }

    [Display(Name = "Sipariş Durumu")]
    public EnumOrderState OrderState { get; set; }
}
public enum EnumOrderState
{
    [Display(Name = "Onay Bekliyor")]
    Waiting,
    [Display(Name = "Onaylandı")]
    Approved,
    [Display(Name = "Reddedildi")]
    Rejected,
    [Display(Name = "Kargoda")]
    Shipped,
    [Display(Name = "Tamamlandı")]
    Completed
}

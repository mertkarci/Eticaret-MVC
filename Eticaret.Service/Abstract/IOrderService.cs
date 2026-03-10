using Eticaret.Core.Entities;
using Eticaret.Service.Concrete;

namespace Eticaret.Service.Abstract;

public interface IOrderService
{
    Task<(bool IsSuccess, string Message, string OrderNumber)> ProcessCheckoutAsync(CheckoutRequest request, CartService cart, AppUser user);
}


public class CheckoutRequest
{
    // Kart Bilgileri
    public string CardNumber { get; set; }
    public string CardNameSurname { get; set; }
    public string CardMonth { get; set; }
    public string CardYear { get; set; }
    public string CVV { get; set; }

    // Üye Kullanıcı Adres Guid'leri
    public string? DeliveryAddressGuid { get; set; }
    public string? BillingAddressGuid { get; set; }

    // MİSAFİR KULLANICI BİLGİLERİ
    public string? GuestName { get; set; }
    public string? GuestSurname { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public string? GuestCity { get; set; }
    public string? GuestDistrict { get; set; }
    public string? GuestOpenAddress { get; set; }
}
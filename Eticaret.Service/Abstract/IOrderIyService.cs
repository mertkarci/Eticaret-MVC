using Eticaret.Core.Entities;
using Eticaret.Service.Concrete;

namespace Eticaret.Service.Abstract
{
    public interface IOrderIyService
    {
        // 1. Aşama: Stokları kontrol eder ve iyzico ödeme formunu (HTML) üretir.
        Task<(bool IsSuccess, string ErrorMessage, string HtmlFormContent)> InitializePaymentAsync(CheckoutRequestIy request, CartService cart, AppUser? user, string ipAddress);

        // 2. Aşama: İyzico'dan dönen token'ı teyit eder, stokları düşer ve Order tablosuna yazar.
        Task<(bool IsSuccess, string Message, string OrderNumber)> FinalizeOrderAsync(string token, CheckoutRequestIy originalRequest, CartService cart, AppUser? user);
    }
}
public class CheckoutRequestIy
{
    // Zorunlu TC Kimlik No
    public string TcNo { get; set; }

    // Üye isek adreslerin Guid'leri
    public string? DeliveryAddressGuid { get; set; }
    public string? BillingAddressGuid { get; set; }

    // Misafir isek manuel doldurulan form bilgileri
    public string? GuestName { get; set; }
    public string? GuestSurname { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public string? GuestCity { get; set; }
    public string? GuestDistrict { get; set; }

    public string? GuestOpenAddress { get; set; }
    public bool IsCorporateInvoice { get; set; } // Fatura tipi seçimi
    public string? CompanyName { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
}

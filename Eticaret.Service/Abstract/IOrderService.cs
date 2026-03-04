using Eticaret.Core.Entities;
using Eticaret.Service.Concrete;

namespace Eticaret.Service.Abstract;

public interface IOrderService
{
    Task<(bool IsSuccess, string Message, string OrderNumber)> ProcessCheckoutAsync(CheckoutRequest request, CartService cart, AppUser user);
}


public class CheckoutRequest
{
    public string CardNumber { get; set; }
    public string CardNameSurname { get; set; }
    public string CardMonth { get; set; }
    public string CardYear { get; set; }
    public string CVV { get; set; }
    public string DeliveryAddressGuid { get; set; }
    public string BillingAddressGuid { get; set; }
}
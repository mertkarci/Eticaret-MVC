using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.Extensions.Configuration;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;

namespace Eticaret.Service.Concrete
{
    public class OrderIyService : IOrderIyService
    {
        private readonly IService<Product> _serviceProduct;
        private readonly IService<Order> _serviceOrder;
        private readonly IService<Eticaret.Core.Entities.Address> _serviceAddress;
        private readonly Iyzipay.Options _iyzicoOptions;

        public OrderIyService(IService<Product> serviceProduct, IService<Order> serviceOrder, IService<Eticaret.Core.Entities.Address> serviceAddress, IConfiguration configuration)
        {
            _serviceProduct = serviceProduct;
            _serviceOrder = serviceOrder;
            _serviceAddress = serviceAddress;

            _iyzicoOptions = new Iyzipay.Options
            {
                ApiKey = configuration["IyzicOptions:ApiKey"],
                SecretKey = configuration["IyzicOptions:SecretKey"],
                BaseUrl = configuration["IyzicOptions:BaseUrl"]
            };
        }

        public async Task<(bool IsSuccess, string ErrorMessage, string HtmlFormContent)> InitializePaymentAsync(CheckoutRequestIy request, CartService cart, AppUser? user, string ipAddress)
        {
            foreach (var item in cart.CartLines)
            {
                var dbProduct = await _serviceProduct.GetAsync(p => p.Id == item.Product.Id);
                if (dbProduct == null) return (false, $"'{item.Product.Name}' artık satışta değil.", string.Empty);

                if (dbProduct.Price != item.Product.Price)
                {
                    cart.RemoveProduct(item.Product);
                    return (false, $"'{item.Product.Name}' için fiyat değişmiş. Ürün sepetinizden çıkarıldı.", string.Empty);
                }

                if (dbProduct.Stock < item.Quantity) return (false, $"'{item.Product.Name}' için yeterli stok bulunmuyor.", string.Empty);
            }

            string deliveryAddressText = "";
            string billingAddressText = "";
            string customerName = "";
            string customerEmail = "";
            string customerPhone = "";
            string customerCity = "";
            string iyzicoBuyerId = "";

            if (user != null)
            {
                var addresses = await _serviceAddress.GetAllAsync(p => p.AppUserId == user.Id && p.IsActive);
                var deliveryAddr = addresses.FirstOrDefault(p => p.AddressGuid.ToString() == request.DeliveryAddressGuid);
                var billingAddr = addresses.FirstOrDefault(p => p.AddressGuid.ToString() == request.BillingAddressGuid);

                if (deliveryAddr == null || billingAddr == null) return (false, "Seçilen adresler geçersiz.", string.Empty);

                deliveryAddressText = $"{deliveryAddr.OpenAddress} {deliveryAddr.District} / {deliveryAddr.City}";
                billingAddressText = $"{billingAddr.OpenAddress} {billingAddr.District} / {billingAddr.City}";
                customerName = $"{user.Name} {user.Surname}";
                customerEmail = user.Email;
                customerPhone = user.Phone ?? "0000000000";
                customerCity = deliveryAddr.City ?? "Istanbul";
                iyzicoBuyerId = "BY" + user.Id;
            }
            else
            {
                deliveryAddressText = $"{request.GuestOpenAddress} {request.GuestDistrict} / {request.GuestCity}";
                billingAddressText = deliveryAddressText;
                customerName = $"{request.GuestName} {request.GuestSurname}";
                customerEmail = request.GuestEmail;
                customerPhone = request.GuestPhone ?? "0000000000";
                customerCity = request.GuestCity ?? "Istanbul";
                iyzicoBuyerId = "GUEST" + Guid.NewGuid().ToString().Substring(0, 8);
            }

            decimal totalCartPrice = cart.TotalPrice();
            if (totalCartPrice < 999) totalCartPrice += 99;
            string formattedPrice = totalCartPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            CreateCheckoutFormInitializeRequest iyziRequest = new CreateCheckoutFormInitializeRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = Guid.NewGuid().ToString(),
                Price = formattedPrice,
                PaidPrice = formattedPrice,
                Currency = Currency.TRY.ToString(),
                BasketId = "B" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                CallbackUrl = "http://localhost:5292/sepetim/odeme-sonuc"
            };

            iyziRequest.Buyer = new Buyer
            {
                Id = iyzicoBuyerId,
                Name = customerName.Split(' ').First(),
                Surname = customerName.Split(' ').LastOrDefault() ?? "Musteri",
                GsmNumber = "+90" + customerPhone,
                Email = customerEmail,
                IdentityNumber = request.IsCorporateInvoice ? request.TaxNumber : request.TcNo,
                RegistrationAddress = deliveryAddressText,
                Ip = string.IsNullOrEmpty(ipAddress) ? "85.34.78.112" : ipAddress,
                City = customerCity,
                Country = "Turkey"
            };

            iyziRequest.ShippingAddress = new Iyzipay.Model.Address
            {
                ContactName = customerName,
                City = customerCity,
                Country = "Turkey",
                Description = deliveryAddressText,
                ZipCode = "34000"
            };
            iyziRequest.BillingAddress = iyziRequest.ShippingAddress;

            List<BasketItem> basketItems = new List<BasketItem>();
            foreach (var item in cart.CartLines)
            {
                basketItems.Add(new BasketItem
                {
                    Id = item.Product.Id.ToString(),
                    Name = item.Product.Name,
                    Category1 = "E-Ticaret",
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = (item.Product.Price * item.Quantity).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                });
            }

            if (cart.TotalPrice() < 999)
            {
                basketItems.Add(new BasketItem { Id = "Kargo", Name = "Kargo Ucreti", Category1 = "Lojistik", ItemType = BasketItemType.VIRTUAL.ToString(), Price = "99.00" });
            }
            iyziRequest.BasketItems = basketItems;

            CheckoutFormInitialize checkoutFormInitialize = await CheckoutFormInitialize.Create(iyziRequest, _iyzicoOptions);

            if (checkoutFormInitialize.Status == "success")
            {
                return (true, string.Empty, checkoutFormInitialize.CheckoutFormContent);
            }

            return (false, checkoutFormInitialize.ErrorMessage, string.Empty);
        }


        public async Task<(bool IsSuccess, string Message, string OrderNumber)> FinalizeOrderAsync(string token, CheckoutRequestIy originalRequest, CartService cart, AppUser? user)
        {
            RetrieveCheckoutFormRequest request = new RetrieveCheckoutFormRequest();
            request.Token = token;
            CheckoutForm checkoutForm = await CheckoutForm.Retrieve(request, _iyzicoOptions);

            if (checkoutForm.PaymentStatus == "SUCCESS")
            {
                string deliveryAddressText = "";
                string billingAddressText = "";
                string customerName = "";
                string customerEmail = "";
                string customerPhone = "";

                if (user != null)
                {
                    var addresses = await _serviceAddress.GetAllAsync(p => p.AppUserId == user.Id && p.IsActive);
                    var deliveryAddr = addresses.FirstOrDefault(p => p.AddressGuid.ToString() == originalRequest.DeliveryAddressGuid);
                    var billingAddr = addresses.FirstOrDefault(p => p.AddressGuid.ToString() == originalRequest.BillingAddressGuid);

                    deliveryAddressText = $"{deliveryAddr?.OpenAddress} {deliveryAddr?.District} / {deliveryAddr?.City}";
                    if (originalRequest.IsCorporateInvoice)
                    {
                        billingAddressText += $" | Kurumsal Fatura -> Firma: {originalRequest.CompanyName} | V.D: {originalRequest.TaxOffice} | V.No: {originalRequest.TaxNumber}";
                    }
                    billingAddressText = $"{billingAddr?.OpenAddress} {billingAddr?.District} / {billingAddr?.City}";
                    customerName = $"{user.Name} {user.Surname}";
                    customerEmail = user.Email;
                    customerPhone = user.Phone ?? "0000000000";
                }
                else
                {
                    deliveryAddressText = $"{originalRequest.GuestOpenAddress} {originalRequest.GuestDistrict} / {originalRequest.GuestCity}";
                    billingAddressText = deliveryAddressText;
                    customerName = $"{originalRequest.GuestName} {originalRequest.GuestSurname}";
                    customerEmail = originalRequest.GuestEmail;
                    customerPhone = originalRequest.GuestPhone ?? "0000000000";
                }

                decimal totalCartPrice = cart.TotalPrice();
                if (totalCartPrice < 999) totalCartPrice += 99;

                var order = new Order
                {
                    AppUserId = user?.Id,
                    CustomerId = user?.UserGuid.ToString(),
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    CustomerPhone = customerPhone,
                    BillingAddress = billingAddressText,
                    DeliveryAddress = deliveryAddressText,
                    BillingTC = originalRequest.TcNo,
                    OrderDate = DateTime.Now,
                    OrderNumber = checkoutForm.PaymentId,
                    TotalPrice = totalCartPrice,
                    OrderState = EnumOrderState.Approved,
                    OrderLines = cart.CartLines.Select(i => new OrderLine
                    {
                        ProductId = i.Product.Id,
                        Quantity = i.Quantity,
                        UnitPrice = i.Product.Price
                    }).ToList()
                };

                foreach (var item in cart.CartLines)
                {
                    var dbProduct = await _serviceProduct.GetAsync(p => p.Id == item.Product.Id);
                    if (dbProduct != null)
                    {
                        dbProduct.Stock -= item.Quantity;
                        _serviceProduct.Update(dbProduct);
                    }
                }

                await _serviceOrder.AddAsync(order);
                await _serviceOrder.SaveChangesAsync();

                return (true, "Başarılı", order.OrderNumber);
            }

            return (false, $"Ödeme reddedildi: {checkoutForm.ErrorMessage}", null);
        }
    }
}
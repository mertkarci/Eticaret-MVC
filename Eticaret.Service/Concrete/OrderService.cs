using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.Extensions.Configuration;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;

namespace Eticaret.Service.Concrete;

public class OrderService : IOrderService
{
    private readonly IService<Product> _serviceProduct;
    private readonly IService<Order> _serviceOrder;
    private readonly IService<Eticaret.Core.Entities.Address> _serviceAddress;
    private readonly IConfiguration _configuration;

    public OrderService(IService<Product> serviceProduct, IService<Order> serviceOrder,
                        IService<Eticaret.Core.Entities.Address> serviceAddress, IConfiguration configuration)
    {
        _serviceProduct = serviceProduct;
        _serviceOrder = serviceOrder;
        _serviceAddress = serviceAddress;
        _configuration = configuration;
    }
    public async Task<(bool IsSuccess, string Message, string OrderNumber)> ProcessCheckoutAsync(CheckoutRequest request, CartService cart, AppUser? user)
    {
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

            if (deliveryAddr == null || billingAddr == null)
                return (false, "Seçilen adresler geçersiz.", null);

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
            billingAddressText = deliveryAddressText; // Misafirlerde fatura ve teslimat adresi aynı kabul edilir

            customerName = $"{request.GuestName} {request.GuestSurname}";
            customerEmail = request.GuestEmail;
            customerPhone = request.GuestPhone ?? "0000000000";
            customerCity = request.GuestCity ?? "Istanbul";
            iyzicoBuyerId = "GUEST" + Guid.NewGuid().ToString().Substring(0, 8); // İyzico boş Id sevmez
        }

        //STOK VE FİYAT KONTROLLERİ
        foreach (var item in cart.CartLines)
        {
            var dbProduct = await _serviceProduct.GetAsync(p => p.Id == item.Product.Id);
            if (dbProduct == null) return (false, $"Sipariş iptal edildi! '{item.Product.Name}' artık satışta değil.", null);

            if (dbProduct.Price != item.Product.Price)
            {
                cart.RemoveProduct(item.Product);
                return (false, $"Sipariş iptal edildi! '{item.Product.Name}' için fiyat değişmiş. Ürün sepetinizden çıkarıldı.", null);
            }

            if (dbProduct.Stock < item.Quantity) return (false, $"Sipariş iptal edildi! '{item.Product.Name}' için yeterli stok bulunmuyor.", null);
        }

        //SİPARİŞ NESNESİ OLUŞTURMA
        var order = new Order
        {
            AppUserId = user?.Id, // Misafirse null kalır
            CustomerId = user?.UserGuid.ToString(), // Misafirse null kalır
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            CustomerPhone = customerPhone,
            BillingAddress = billingAddressText,
            DeliveryAddress = deliveryAddressText,
            OrderDate = DateTime.Now,
            OrderNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
            TotalPrice = cart.TotalPrice(),
            OrderState = EnumOrderState.Waiting,
            OrderLines = cart.CartLines.Select(i => new OrderLine
            {
                ProductId = i.Product.Id,
                Quantity = i.Quantity,
                UnitPrice = i.Product.Price
            }).ToList()
        };

        if (order.TotalPrice < 999) order.TotalPrice += 99;



        //İYZİCO ÖDEME ALTYAPISI
        Iyzipay.Options iyzicoOptions = new Iyzipay.Options
        {
            ApiKey = _configuration["IyzicOptions:ApiKey"],
            SecretKey = _configuration["IyzicOptions:SecretKey"],
            BaseUrl = _configuration["IyzicOptions:BaseUrl"]
        };

        CreatePaymentRequest iyziRequest = new CreatePaymentRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = Guid.NewGuid().ToString(),
            Price = order.TotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            PaidPrice = order.TotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            Currency = Currency.TRY.ToString(),
            Installment = 1,
            BasketId = "B" + order.OrderNumber,
            PaymentChannel = PaymentChannel.WEB.ToString(),
            PaymentGroup = PaymentGroup.PRODUCT.ToString()
        };

        string cleanCardNumber = request.CardNumber.Replace(" ", "").Replace("-", "");
        iyziRequest.PaymentCard = new PaymentCard
        {
            CardHolderName = request.CardNameSurname,
            CardNumber = cleanCardNumber,
            ExpireMonth = request.CardMonth.Trim().PadLeft(2, '0'),
            ExpireYear = request.CardYear,
            Cvc = request.CVV.Trim(),
            RegisterCard = 0
        };

        iyziRequest.Buyer = new Buyer
        {
            Id = iyzicoBuyerId,
            Name = customerName.Split(' ').First(),
            Surname = customerName.Split(' ').LastOrDefault() ?? "Musteri",
            GsmNumber = "+90" + customerPhone,
            Email = customerEmail,
            IdentityNumber = "11111111111",
            RegistrationAddress = deliveryAddressText,
            Ip = "85.34.78.112",
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

        Payment payment = await Payment.Create(iyziRequest, iyzicoOptions);

        //SONUÇ VE KAYIT
        if (payment.Status == "success")
        {
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

        return (false, $"Ödeme reddedildi: {payment.ErrorMessage}", null);
    }
    // public async Task<(bool IsSuccess, string Message, string OrderNumber)> ProcessCheckoutAsync(CheckoutRequest request, CartService cart, AppUser user)
    // {
    //     // 1. ADRES DOĞRULAMA
    //     var addresses = await _serviceAddress.GetAllAsync(p => p.AppUserId == user.Id && p.IsActive);
    //     var deliveryAddr = addresses.FirstOrDefault(p => p.AddressGuid.ToString() == request.DeliveryAddressGuid);
    //     var billingAddr = addresses.FirstOrDefault(p => p.AddressGuid.ToString() == request.BillingAddressGuid);

    //     if (deliveryAddr == null || billingAddr == null)
    //         return (false, "Seçilen adresler geçersiz.", null);

    //     // 2. KESİN STOK KONTROLÜ
    //     foreach (var item in cart.CartLines)
    //     {
    //         var dbProduct = await _serviceProduct.GetAsync(p => p.Id == item.Product.Id);

    //         // Ürün veritabanında yoksa
    //         if (dbProduct == null)
    //         {
    //             cart.RemoveProduct(item.Product);
    //             return (false, $"Sipariş iptal edildi! '{item.Product.Name}' artık satışta değil.", null);
    //         }

    //         Console.WriteLine("--------------------------------------------------");
    //         Console.WriteLine($"URUN: {item.Product.Name}");
    //         Console.WriteLine($"DB FIYAT: {dbProduct.Price}");
    //         Console.WriteLine($"SEPET FIYAT: {item.Product.Price}");
    //         Console.WriteLine("--------------------------------------------------");

    //         // Fiyat Kontrolü
    //         if (dbProduct.Price != item.Product.Price)
    //         {

    //             Console.WriteLine($"!!! FIYAT FARKI TESPIT EDILDI: {dbProduct.Price - item.Product.Price}");

    //             cart.RemoveProduct(item.Product);
    //             return (false, $"Sipariş iptal edildi! '{item.Product.Name}' için yeni bir fiyat belirtilmiş. Ürün sepetinizden çıkarıldı.", null);
    //         }

    //         // Stok Kontrolü
    //         if (dbProduct.Stock < item.Quantity)
    //         {
    //             return (false, $"Sipariş iptal edildi! '{item.Product.Name}' için yeterli stok bulunmuyor.", null);
    //         }
    //     }

    //     // 3. SİPARİŞ NESNESİ OLUŞTURMA
    //     var order = new Order
    //     {
    //         AppUserId = user.Id,
    //         CustomerId = user.UserGuid.ToString(),
    //         BillingAddress = $"{billingAddr.OpenAddress} {billingAddr.District} / {billingAddr.City}",
    //         DeliveryAddress = $"{deliveryAddr.OpenAddress} {deliveryAddr.District} / {deliveryAddr.City}",
    //         OrderDate = DateTime.Now,
    //         OrderNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
    //         TotalPrice = cart.TotalPrice(),
    //         OrderState = EnumOrderState.Waiting,
    //         OrderLines = cart.CartLines.Select(i => new OrderLine
    //         {
    //             ProductId = i.Product.Id,
    //             Quantity = i.Quantity,
    //             UnitPrice = i.Product.Price
    //         }).ToList()
    //     };

    //     if (order.TotalPrice < 999)
    //     {
    //         order.TotalPrice += 99;
    //     }

    //     #region Iyzico Payment Process
    //     Iyzipay.Options iyzicoOptions = new Iyzipay.Options
    //     {
    //         ApiKey = _configuration["IyzicOptions:ApiKey"],
    //         SecretKey = _configuration["IyzicOptions:SecretKey"],
    //         BaseUrl = _configuration["IyzicOptions:BaseUrl"]
    //     };

    //     CreatePaymentRequest iyziRequest = new CreatePaymentRequest
    //     {
    //         Locale = Locale.TR.ToString(),
    //         ConversationId = Guid.NewGuid().ToString(),
    //         Price = order.TotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
    //         PaidPrice = order.TotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
    //         Currency = Currency.TRY.ToString(),
    //         Installment = 1,
    //         BasketId = "B" + order.OrderNumber,
    //         PaymentChannel = PaymentChannel.WEB.ToString(),
    //         PaymentGroup = PaymentGroup.PRODUCT.ToString()
    //     };

    //     string cleanCardNumber = request.CardNumber.Replace(" ", "").Replace("-", "");
    //     PaymentCard paymentCard = new PaymentCard
    //     {
    //         CardHolderName = request.CardNameSurname,
    //         CardNumber = cleanCardNumber,
    //         ExpireMonth = request.CardMonth.Trim().PadLeft(2, '0'),
    //         ExpireYear = request.CardYear,
    //         Cvc = request.CVV.Trim(),
    //         RegisterCard = 0
    //     };
    //     iyziRequest.PaymentCard = paymentCard;

    //     Buyer buyer = new Buyer
    //     {
    //         Id = "BY" + user.Id,
    //         Name = user.Name ?? "Musteri",
    //         Surname = user.Surname ?? "Soyadi",
    //         GsmNumber = "+90" + user.Phone,
    //         Email = user.Email,
    //         IdentityNumber = "11111111111",
    //         RegistrationAddress = order.DeliveryAddress,
    //         Ip = "85.34.78.112",
    //         City = deliveryAddr.City ?? "Istanbul",
    //         Country = "Turkey",
    //         ZipCode = "34000"
    //     };
    //     iyziRequest.Buyer = buyer;

    //     Iyzipay.Model.Address shippingAddress = new Iyzipay.Model.Address
    //     {
    //         ContactName = user.Name + " " + user.Surname,
    //         City = deliveryAddr.City ?? "Istanbul",
    //         Country = "Turkey",
    //         Description = deliveryAddr.OpenAddress,
    //         ZipCode = "34000"
    //     };
    //     iyziRequest.ShippingAddress = shippingAddress;

    //     Iyzipay.Model.Address billingAddress = new Iyzipay.Model.Address
    //     {
    //         ContactName = user.Name + " " + user.Surname,
    //         City = billingAddr.City ?? "Istanbul",
    //         Country = "Turkey",
    //         Description = billingAddr.OpenAddress,
    //         ZipCode = "34000"
    //     };
    //     iyziRequest.BillingAddress = billingAddress;

    //     List<BasketItem> basketItems = new List<BasketItem>();
    //     foreach (var item in cart.CartLines)
    //     {
    //         basketItems.Add(new BasketItem
    //         {
    //             Id = item.Product.Id.ToString(),
    //             Name = item.Product.Name,
    //             Category1 = "E-Ticaret",
    //             ItemType = BasketItemType.PHYSICAL.ToString(),
    //             Price = (item.Product.Price * item.Quantity).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
    //         });
    //     }

    //     if (cart.TotalPrice() < 999)
    //     {
    //         basketItems.Add(new BasketItem
    //         {
    //             Id = "Kargo",
    //             Name = "Kargo Ucreti",
    //             Category1 = "Lojistik",
    //             ItemType = BasketItemType.VIRTUAL.ToString(),
    //             Price = "99.00"
    //         });
    //     }
    //     iyziRequest.BasketItems = basketItems;

    //     Payment payment = await Payment.Create(iyziRequest, iyzicoOptions);
    //     #endregion

    //     if (payment.Status == "success")
    //     {
    //         foreach (var item in cart.CartLines)
    //         {
    //             var dbProduct = await _serviceProduct.GetAsync(p => p.Id == item.Product.Id);
    //             if (dbProduct != null)
    //             {
    //                 dbProduct.Stock -= item.Quantity;
    //                 _serviceProduct.Update(dbProduct);
    //             }
    //         }

    //         await _serviceOrder.AddAsync(order);
    //         await _serviceOrder.SaveChangesAsync();

    //         return (true, "Başarılı", order.OrderNumber);
    //     }

    //     return (false, $"Ödeme reddedildi: {payment.ErrorMessage}", null);
    // }
}
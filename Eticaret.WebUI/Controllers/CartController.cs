using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Eticaret.Core.Entities;
using Eticaret.Service;
using Eticaret.Service.Abstract;
using Eticaret.Service.Concrete;
using Eticaret.WebUI.ExtensionMethods;
using Eticaret.WebUI.Models;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eticaret.WebUI.Controllers
{
    public class CartController : Controller
    {
        private readonly IService<Product> _serviceProduct;
        private readonly IService<AppUser> _serviceAppUser;
        private readonly IService<Core.Entities.Address> _serviceAddress;
        private readonly IService<Order> _serviceOrder;
        private readonly IConfiguration _configuration;

        public CartController(IService<Product> serviceProduct, IService<AppUser> serviceAppUser, IService<Core.Entities.Address> serviceAddress, IService<Order> serviceOrder, IConfiguration configuration)
        {
            _serviceProduct = serviceProduct;
            _serviceAppUser = serviceAppUser;
            _serviceAddress = serviceAddress;
            _serviceOrder = serviceOrder;
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            var cart = GetCart();
            var model = new CartViewModel()
            {
                CartLines = cart.CartLines,
                TotalPrice = cart.TotalPrice()
            };
            return View(model);
        }
        public IActionResult Add(int ProductId, int quantity = 1)
        {
            var product = _serviceProduct.Find(ProductId);
            if (product != null)

            {
                var cart = GetCart();
                cart.AddProduct(product, quantity);
                HttpContext.Session.SetJson("Cart", cart);
                return Redirect(Request.Headers["Referer"].ToString());
            }
            return RedirectToAction("Index");
        }
        public IActionResult Update(int ProductId, int quantity = 1)
        {
            var product = _serviceProduct.Find(ProductId);
            if (product != null)

            {
                var cart = GetCart();
                cart.UpdateProduct(product, quantity);
                HttpContext.Session.SetJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }
        public IActionResult Remove(int ProductId)
        {
            var product = _serviceProduct.Find(ProductId);
            if (product != null)

            {
                var cart = GetCart();
                cart.RemoveProduct(product);
                HttpContext.Session.SetJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            var userGuidClaim = HttpContext.User.FindFirst("UserGuid");

            if (userGuidClaim == null)
            {
                return RedirectToAction("SignIn", "Accounts");
            }

            //String olan claim değerini C# Guid nesnesine dönüştür
            if (!Guid.TryParse(userGuidClaim.Value, out Guid parsedGuid))
            {
                // Eğer cookie'deki değer bozuksa veya guid'e çevrilemiyorsa yine logine at
                return RedirectToAction("SignIn", "Accounts");
            }

            // 3. Artık veritabanı sorgusunu ToString() KULLANMADAN, direkt Guid üzerinden yapıyoruz
            var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid == parsedGuid);

            if (appUser == null)
            {
                return RedirectToAction("SignIn", "Accounts");
            }

            // Kullanıcıyı bulduk, adreslerini çekiyoruz
            var addresses = await _serviceAddress.GetAllAsync(p => p.AppUserId == appUser.Id && p.IsActive);

            var model = new CheckoutViewModel()
            {
                CartProducts = cart.CartLines,
                TotalPrice = cart.TotalPrice(),
                Addresses = addresses
            };

            return View(model);
        }
        private CartService GetCart()
        {
            return HttpContext.Session.GetJson<CartService>("Cart") ?? new CartService();


        }
        [Authorize, HttpPost]
        public async Task<IActionResult> Checkout(string CardNumber, string CardNameSurname, string CardMonth, string CardYear, string CVV, string DeliveryAddress, string BillingAddress)
        {
            var cart = GetCart();

            var userGuidClaim = HttpContext.User.FindFirst("UserGuid");
            if (userGuidClaim == null || !Guid.TryParse(userGuidClaim.Value, out Guid parsedGuid))
            {
                return RedirectToAction("SignIn", "Accounts");
            }

            var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid == parsedGuid);
            if (appUser == null)
            {
                return RedirectToAction("SignIn", "Accounts");
            }

            var addresses = await _serviceAddress.GetAllAsync(p => p.AppUserId == appUser.Id && p.IsActive);

            var model = new CheckoutViewModel()
            {
                CartProducts = cart.CartLines,
                TotalPrice = cart.TotalPrice(),
                Addresses = addresses
            };

            if (string.IsNullOrWhiteSpace(CardNumber) || string.IsNullOrWhiteSpace(CardYear) || string.IsNullOrWhiteSpace(CardMonth) || string.IsNullOrWhiteSpace(CVV) || string.IsNullOrWhiteSpace(DeliveryAddress) || string.IsNullOrWhiteSpace(BillingAddress))
            {
                TempData["Message"] = "Lütfen tüm bilgileri eksiksiz doldurun.";
                return View(model);
            }

            var deliveryAddress = addresses.FirstOrDefault(p => p.AddressGuid.ToString() == DeliveryAddress);
            var _billingAddress = addresses.FirstOrDefault(p => p.AddressGuid.ToString() == BillingAddress);

            if (deliveryAddress == null || _billingAddress == null)
            {
                TempData["Message"] = "Seçilen adresler geçersiz.";
                return View(model);
            }


            //KESİN STOK KONTROLÜ (İyzico'ya gitmeden ve Order oluşturmadan ÖNCE!)

            foreach (var item in cart.CartLines)
            {
                // Veritabanındaki en güncel stok bilgisi
                var dbProduct = await _serviceProduct.GetAsync(p => p.Id == item.Product.Id);

                // Eğer ürün silinmişse veya stoğu kullanıcının sepetindeki adetten azsa (Örn: 0 < 1)
                if (dbProduct == null || dbProduct.Stock < item.Quantity)
                {

                    TempData["Message"] = $"Sipariş iptal edildi! Sepetinizdeki '{item.Product.Name}' ürünü için yeterli stok bulunmuyor. (Mevcut Stok: {(dbProduct?.Stock ?? 0)})";
                    return View(model); // Metot burada çalışmayı durdurur, İyzico'ya asla gitmez.
                }
            }


            var order = new Order
            {
                AppUserId = appUser.Id,
                BillingAddress = $"{_billingAddress.OpenAddress}{_billingAddress.City}{_billingAddress.District}",
                DeliveryAddress = $"{deliveryAddress.OpenAddress}{deliveryAddress.City}{deliveryAddress.District}",
                CustomerId = appUser.UserGuid.ToString(),
                OrderDate = DateTime.Now,
                OrderNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(), // Sipariş numarası çok uzun olmasın diye
                TotalPrice = cart.TotalPrice(),
                OrderState = EnumOrderState.Waiting,
                OrderLines = new List<OrderLine>()
            };


            #region CheckoutProcess
            Options options = new Options();
            options.ApiKey = _configuration["IyzicOptions:ApiKey"];
            options.SecretKey = _configuration["IyzicOptions:SecretKey"];
            options.BaseUrl = _configuration["IyzicOptions:BaseUrl"];

            CreatePaymentRequest request = new CreatePaymentRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = HttpContext.Session.Id;

            request.Price = order.TotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            request.PaidPrice = order.TotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            request.Currency = Currency.TRY.ToString();
            request.Installment = 1;
            request.BasketId = "B" + order.OrderNumber;
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

            // Kart Numarasındaki boşlukları temizle
            string cleanCardNumber = CardNumber.Replace(" ", "").Replace("-", "");

            PaymentCard paymentCard = new PaymentCard();
            paymentCard.CardHolderName = CardNameSurname;
            paymentCard.CardNumber = cleanCardNumber;
            paymentCard.ExpireMonth = CardMonth.Trim().PadLeft(2, '0'); // 5'i 05 yapar
            paymentCard.ExpireYear = CardYear;
            paymentCard.Cvc = CVV.Trim();
            paymentCard.RegisterCard = 0;
            request.PaymentCard = paymentCard;

            Buyer buyer = new Buyer();
            buyer.Id = "BY" + appUser.Id;
            buyer.Name = appUser.Name ?? "Müşteri";
            buyer.Surname = appUser.Surname ?? "Müşteri";
            buyer.GsmNumber = "+905350000000";
            buyer.Email = appUser.Email;
            buyer.IdentityNumber = "11111111111";

            buyer.LastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            buyer.RegistrationDate = appUser.CreateDate.ToString("yyyy-MM-dd HH:mm:ss");
            buyer.RegistrationAddress = order.DeliveryAddress;

            buyer.Ip =  HttpContext.Connection.RemoteIpAddress?.ToString();//"85.34.78.112";
            buyer.City = deliveryAddress.City ?? "Istanbul";
            buyer.Country = "Turkey";
            buyer.ZipCode = "34732";
            request.Buyer = buyer;

            var shippingAddress = new Iyzipay.Model.Address();
            shippingAddress.ContactName = appUser.Name + " " + appUser.Surname;
            shippingAddress.City = deliveryAddress.City ?? "Istanbul";
            shippingAddress.Country = "Turkey";
            shippingAddress.Description = deliveryAddress.OpenAddress;
            shippingAddress.ZipCode = "34732";
            request.ShippingAddress = shippingAddress;

            var billingAddress = new Iyzipay.Model.Address();
            billingAddress.ContactName = appUser.Name + " " + appUser.Surname;
            billingAddress.City = _billingAddress.City ?? "Istanbul";
            billingAddress.Country = "Turkey";
            billingAddress.Description = _billingAddress.OpenAddress;
            billingAddress.ZipCode = "34732";
            request.BillingAddress = billingAddress;

            List<BasketItem> basketItems = new List<BasketItem>();

            foreach (var item in cart.CartLines)
            {
                order.OrderLines.Add(new OrderLine
                {
                    ProductId = item.Product.Id,
                    OrderId = order.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price,
                });

                decimal itemTotalPrice = item.Product.Price * item.Quantity;

                basketItems.Add(new BasketItem
                {
                    Id = item.Product.Id.ToString(),
                    Name = item.Product.Name,
                    Category1 = "Collectibles",
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = itemTotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                });
            }

            if (order.TotalPrice < 999)
            {
                basketItems.Add(new BasketItem
                {
                    Id = "Kargo",
                    Name = "Kargo Ücreti",
                    Category1 = "Kargo Ücreti",
                    ItemType = BasketItemType.VIRTUAL.ToString(),
                    Price = "99.00"
                });

                order.TotalPrice += 99;

                request.Price = order.TotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                request.PaidPrice = order.TotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            }

            request.BasketItems = basketItems;
            Payment payment = await Payment.Create(request, options);
            #endregion

            try
            {
                if (payment.Status == "success")
                {
                    // BAŞARILI ÖDEME SONRASI STOKLARI DÜŞ VE KAYDET

                    foreach (var item in cart.CartLines)
                    {
                        var dbProduct = await _serviceProduct.GetAsync(p => p.Id == item.Product.Id);
                        if (dbProduct != null)
                        {
                            dbProduct.Stock -= item.Quantity; // Stoktan düş
                            _serviceProduct.Update(dbProduct); // Ürünü güncelle
                        }
                    }

                    // Siparişi ve güncellenen stokları tek seferde veritabanına kaydet
                    await _serviceOrder.AddAsync(order);
                    var result = await _serviceOrder.SaveChangesAsync();

                    if (result > 0)
                    {
                        HttpContext.Session.Remove("Cart"); // İşlem bitince sepeti temizle
                        return RedirectToAction("Thanks");
                    }
                }
                else
                {
                    TempData["Message"] = $"Ödeme reddedildi: {payment.ErrorMessage} (Hata Kodu: {payment.ErrorCode})";
                }
            }
            catch (Exception)
            {
                TempData["Message"] = "İşlem sırasında bir hata oluştu. Lütfen tekrar deneyin.";
            }

            return View(model);
        }

        public IActionResult Thanks()
        {
            return View();
        }
    }
}
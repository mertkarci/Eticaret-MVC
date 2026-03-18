using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Eticaret.Service.Concrete;
using Eticaret.WebUI.ExtensionMethods;
using Eticaret.WebUI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Eticaret.WebUI.Controllers
{
    [Route("sepetim")]
    public class CartController : Controller
    {
        private readonly IService<Product> _serviceProduct;
        private readonly IService<AppUser> _serviceAppUser;
        private readonly IService<Address> _serviceAddress;

        // 🚨 Eski IOrderService silindi, yerine IOrderIyService geldi
        private readonly IOrderIyService _orderIyService;

        public CartController(IService<Product> serviceProduct, IService<AppUser> serviceAppUser, IService<Address> serviceAddress, IOrderIyService orderIyService)
        {
            _serviceProduct = serviceProduct;
            _serviceAppUser = serviceAppUser;
            _serviceAddress = serviceAddress;
            _orderIyService = orderIyService;
        }

        private CartService GetCart()
        {
            return HttpContext.Session.GetJson<CartService>("Cart") ?? new CartService();
        }

        [HttpGet("")]
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

        [HttpPost("ekle")]
        [EnableRateLimiting("CartLimit")]
        public IActionResult Add(int ProductId, int quantity = 1)
        {
            var product = _serviceProduct.Find(ProductId);
            if (product != null)
            {
                var cart = GetCart();
                cart.AddProduct(product, quantity);
                HttpContext.Session.SetJson("Cart", cart);

                var returnUrl = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost("guncelle")]
        [EnableRateLimiting("CartLimit")]
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

        [HttpPost("sil")]
        [EnableRateLimiting("CartLimit")]
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

        [HttpGet("ödeme")]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            var model = new CheckoutViewModel()
            {
                CartProducts = cart.CartLines,
                TotalPrice = cart.TotalPrice(),
                Addresses = new List<Address>()     
            };

            if (User.Identity.IsAuthenticated)
            {
                var userGuidClaim = HttpContext.User.FindFirst("UserGuid");

                if (userGuidClaim == null || !Guid.TryParse(userGuidClaim.Value, out Guid parsedGuid))
                {
                    await HttpContext.SignOutAsync();
                    return RedirectToAction("SignIn", "Accounts");
                }

                var appUser = await _serviceAppUser.GetAsync(p => p.UserGuid == parsedGuid);

                if (appUser == null)
                {
                    await HttpContext.SignOutAsync();
                    return RedirectToAction("SignIn", "Accounts");
                }

                model.Addresses = await _serviceAddress.GetAllAsync(p => p.AppUserId == appUser.Id && p.IsActive);
            }

            return View(model);
        }

        [HttpPost("ödeme")]
        [EnableRateLimiting("CheckoutLimit")]
        public async Task<IActionResult> Checkout(CheckoutRequestIy requestModel)
        {
            var cart = GetCart();
            if (cart.CartLines.Count == 0)
            {
                TempData["Message"] = "Ödeme yapabilmek için sepetinizde ürün bulunmalıdır.";
                return RedirectToAction("Index");
            }

            AppUser? userToProcess = null;

            if (User.Identity.IsAuthenticated)
            {
                var userGuidClaim = HttpContext.User.FindFirst("UserGuid");
                if (userGuidClaim != null && Guid.TryParse(userGuidClaim.Value, out var parsedGuid))
                {
                    userToProcess = await _serviceAppUser.GetAsync(p => p.UserGuid == parsedGuid);
                }

                // Giriş yapmış kullanıcının adres seçtiğinden emin olalım
                if (string.IsNullOrEmpty(requestModel.DeliveryAddressGuid) || string.IsNullOrEmpty(requestModel.BillingAddressGuid))
                {
                    TempData["Message"] = "Lütfen teslimat ve fatura adresi seçtiğinizden emin olun.";
                    return RedirectToAction("Checkout");
                }
            }
            else // MİSAFİR İŞLEMİ
            {
                if (string.IsNullOrWhiteSpace(requestModel.GuestName) ||
                    string.IsNullOrWhiteSpace(requestModel.GuestEmail) ||
                    string.IsNullOrWhiteSpace(requestModel.GuestOpenAddress))
                {
                    TempData["Message"] = "Lütfen iletişim ve adres bilgilerinizi eksiksiz doldurun.";
                    return RedirectToAction("Checkout");
                }
            }

            // FATURA BİLGİSİ DOĞRULAMASI (HEM GİRİŞ YAPMIŞ HEM MİSAFİR İÇİN ORTAK ALANA ALINDI)
            if (requestModel.IsCorporateInvoice)
            {
                if (string.IsNullOrWhiteSpace(requestModel.CompanyName) ||
                    string.IsNullOrWhiteSpace(requestModel.TaxOffice) ||
                    string.IsNullOrWhiteSpace(requestModel.TaxNumber))
                {
                    TempData["Message"] = "Kurumsal fatura için Firma Adı, Vergi Dairesi ve Vergi Numarası eksiksiz girilmelidir.";
                    return RedirectToAction("Checkout");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(requestModel.TcNo))
                {
                    TempData["Message"] = "Bireysel fatura için TC Kimlik Numarası zorunludur.";
                    return RedirectToAction("Checkout");
                }
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "85.34.78.112";

            var result = await _orderIyService.InitializePaymentAsync(requestModel, cart, userToProcess, ipAddress);

            if (result.IsSuccess)
            {
                HttpContext.Session.SetJson("PendingCheckoutData", requestModel);

                ViewBag.IyzicoForm = result.HtmlFormContent;
                return View("PaymentPage");
            }

            TempData["Message"] = "Ödeme altyapısına bağlanılamadı: " + result.ErrorMessage;
            return RedirectToAction("Checkout");
        }

        [HttpPost("odeme-sonuc")]
        [IgnoreAntiforgeryToken] 
        public async Task<IActionResult> PaymentCallback([FromForm] string token)
        {
            var cart = GetCart();

            var originalRequest = HttpContext.Session.GetJson<CheckoutRequestIy>("PendingCheckoutData");

            if (originalRequest == null || string.IsNullOrEmpty(token))
            {
                TempData["Message"] = "Ödeme oturumunuz zaman aşımına uğradı. Lütfen siparişinizi tekrar oluşturun.";
                return RedirectToAction("Checkout");
            }

            AppUser? userToProcess = null;
            if (User.Identity.IsAuthenticated)
            {
                var userGuidClaim = HttpContext.User.FindFirst("UserGuid");
                if (userGuidClaim != null && Guid.TryParse(userGuidClaim.Value, out var parsedGuid))
                {
                    userToProcess = await _serviceAppUser.GetAsync(p => p.UserGuid == parsedGuid);
                }
            }

            var result = await _orderIyService.FinalizeOrderAsync(token, originalRequest, cart, userToProcess);

            if (result.IsSuccess)
            {
                HttpContext.Session.Remove("Cart");
                HttpContext.Session.Remove("PendingCheckoutData");

                return RedirectToAction("Thanks", new { orderNumber = result.OrderNumber });
            }

            TempData["Message"] = "Ödeme işlemi başarısız: " + result.Message;
            return RedirectToAction("Checkout"); 
        }

        [HttpGet("basarili")]
        public IActionResult Thanks(string orderNumber)
        {
            if (!string.IsNullOrEmpty(orderNumber))
            {
                TempData["OrderNumber"] = orderNumber;
            }
            return View();
        }
    }
}
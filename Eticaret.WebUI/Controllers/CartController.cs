
using Eticaret.Core.Entities;

using Eticaret.Service.Abstract;
using Eticaret.Service.Concrete;
using Eticaret.WebUI.ExtensionMethods;
using Eticaret.WebUI.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Eticaret.WebUI.Controllers
{
    [Route("sepetim")]
    public class CartController : Controller
    {
        private readonly IService<Product> _serviceProduct;
        private readonly IService<AppUser> _serviceAppUser;
        private readonly IService<Address> _serviceAddress;

        private readonly IOrderService _orderService;


        public CartController(IService<Product> serviceProduct, IService<AppUser> serviceAppUser, IService<Address> serviceAddress, IOrderService orderService)
        {
            _serviceProduct = serviceProduct;
            _serviceAppUser = serviceAppUser;
            _serviceAddress = serviceAddress;

            _orderService = orderService;

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
                Addresses = new List<Address>() // Default to empty list for guests
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
        private CartService GetCart()
        {
            return HttpContext.Session.GetJson<CartService>("Cart") ?? new CartService();
        }
        [HttpPost("ödeme")]
        public async Task<IActionResult> Checkout(
            // Kart Bilgileri
            string CardNumber, string CardNameSurname, string CardMonth, string CardYear, string CVV,
            // Üye Adres Bilgileri
            string DeliveryAddress, string BillingAddress,
            // Misafir Bilgileri
            string GuestName, string GuestSurname, string GuestEmail, string GuestPhone,
            string GuestCity, string GuestDistrict, string GuestOpenAddress)
        {
            var cart = GetCart();
            if (cart.CartLines.Count == 0)
            {
                TempData["Message"] = "Ödeme yapabilmek için sepetinizde ürün bulunmalıdır.";
                return RedirectToAction("Index");
            }

    
            AppUser? userToProcess = null;
            var requestModel = new CheckoutRequest
            {
                CardNumber = CardNumber,
                CardNameSurname = CardNameSurname,
                CardMonth = CardMonth,
                CardYear = CardYear,
                CVV = CVV
            };

            // KULLANICI KONTROLÜ 
            if (User.Identity.IsAuthenticated)
            {
                var userGuidClaim = HttpContext.User.FindFirst("UserGuid");
                if (userGuidClaim == null || !Guid.TryParse(userGuidClaim.Value, out var parsedGuid))
                {
                    return RedirectToAction("SignIn", "Accounts");
                }

                userToProcess = await _serviceAppUser.GetAsync(p => p.UserGuid == parsedGuid);

                requestModel.DeliveryAddressGuid = DeliveryAddress;
                requestModel.BillingAddressGuid = BillingAddress;
            }
            else // MİSAFİR İŞLEMİ
            {
                if (string.IsNullOrWhiteSpace(GuestName) || string.IsNullOrWhiteSpace(GuestEmail) || string.IsNullOrWhiteSpace(GuestOpenAddress))
                {
                    TempData["Message"] = "Lütfen iletişim ve adres bilgilerinizi eksiksiz doldurun.";
                    return RedirectToAction("Checkout");
                }
                requestModel.GuestName = GuestName;
                requestModel.GuestSurname = GuestSurname;
                requestModel.GuestEmail = GuestEmail;
                requestModel.GuestPhone = GuestPhone;
                requestModel.GuestCity = GuestCity;
                requestModel.GuestDistrict = GuestDistrict;
                requestModel.GuestOpenAddress = GuestOpenAddress;
            }

            var result = await _orderService.ProcessCheckoutAsync(requestModel, cart, userToProcess);

            if (result.IsSuccess)
            {
                HttpContext.Session.Remove("Cart");
                TempData["OrderNumber"] = result.OrderNumber;
                return RedirectToAction("Thanks", new { orderNumber = result.OrderNumber });
            }

            TempData["Message"] = result.Message;
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
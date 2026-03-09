
using Eticaret.Core.Entities;

using Eticaret.Service.Abstract;
using Eticaret.Service.Concrete;
using Eticaret.WebUI.ExtensionMethods;
using Eticaret.WebUI.Models;

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
        [Authorize]
        [HttpGet("ödeme")]
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
        [Authorize, HttpPost("ödeme")]
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

            var requestModel = new CheckoutRequest
            {
                CardNumber = CardNumber,
                CardNameSurname = CardNameSurname,
                CardMonth = CardMonth,
                CardYear = CardYear,
                CVV = CVV,
                DeliveryAddressGuid = DeliveryAddress,
                BillingAddressGuid = BillingAddress
            };

            var result = await _orderService.ProcessCheckoutAsync(requestModel, cart, appUser);

            if (result.IsSuccess)
            {
                HttpContext.Session.Remove("Cart");
                TempData["OrderNumber"] = result.OrderNumber;
                return RedirectToAction("Thanks");
            }
            HttpContext.Session.SetJson("Cart", cart);
            TempData["Message"] = result.Message;

            var addresses = await _serviceAddress.GetAllAsync(p => p.AppUserId == appUser.Id && p.IsActive);
            var model = new CheckoutViewModel
            {
                CartProducts = cart.CartLines,
                TotalPrice = cart.TotalPrice(),
                Addresses = addresses
            };
            return View(model);
        }
        [HttpGet("basarili")]
        public IActionResult Thanks()
        {
            return View();
        }
    }
}
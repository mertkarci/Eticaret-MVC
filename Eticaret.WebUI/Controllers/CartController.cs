using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Eticaret.Core.Entities;
using Eticaret.Service;
using Eticaret.Service.Abstract;
using Eticaret.Service.Concrete;
using Eticaret.WebUI.ExtensionMethods;
using Eticaret.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eticaret.WebUI.Controllers
{
    public class CartController : Controller
    {
        private readonly IService<Product> _serviceProduct;
        private readonly IService<AppUser> _serviceAppUser;
        private readonly IService<Address> _serviceAddress;
        private readonly IService<Order> _serviceOrder;

        public CartController(IService<Product> serviceProduct, IService<AppUser> serviceAppUser, IService<Address> serviceAddress, IService<Order> serviceOrder)
        {
            _serviceProduct = serviceProduct;
            _serviceAppUser = serviceAppUser;
            _serviceAddress = serviceAddress;
            _serviceOrder = serviceOrder;


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

            // 1. Önce Claim'i güvenli bir şekilde yakala
            var userGuidClaim = HttpContext.User.FindFirst("UserGuid");

            if (userGuidClaim == null)
            {
                return RedirectToAction("SignIn", "Accounts");
            }

            // 2. String olan claim değerini C# Guid nesnesine dönüştür (En kritik nokta!)
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
        public async Task<IActionResult> Checkout(string CardNumber, string CardMonth, string CardYear, string CVV, string DeliveryAddress, string BillingAddress)
        {
            var cart = GetCart();

            // 1. Önce Claim'i güvenli bir şekilde yakala
            var userGuidClaim = HttpContext.User.FindFirst("UserGuid");

            if (userGuidClaim == null)
            {
                return RedirectToAction("SignIn", "Accounts");
            }

            // 2. String olan claim değerini C# Guid nesnesine dönüştür (En kritik nokta!)
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
            if (string.IsNullOrWhiteSpace(CardNumber) || string.IsNullOrWhiteSpace(CardYear) || string.IsNullOrWhiteSpace(CardMonth) || string.IsNullOrWhiteSpace(CVV) || string.IsNullOrWhiteSpace(DeliveryAddress) || string.IsNullOrWhiteSpace(BillingAddress))
            {

            }
            var deliveryAddress = addresses.FirstOrDefault(p => p.AddressGuid.ToString() == DeliveryAddress);
            var billingAddress = addresses.FirstOrDefault(p => p.AddressGuid.ToString() == BillingAddress);
            //
            var order = new Order
            {
                AppUserId = appUser.Id,
                BillingAddress = BillingAddress,
                DeliveryAddress = DeliveryAddress,
                CustomerId = appUser.UserGuid.ToString(),
                OrderDate = DateTime.Now,
                OrderNumber = Guid.NewGuid().ToString(),
                TotalPrice = cart.TotalPrice(),
                OrderLines = []
            };

            foreach (var item in cart.CartLines)
            {
                order.OrderLines.Add(new OrderLine
                {
                    ProductId = item.Product.Id,
                    OrderId = order.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price,
                });
            }

            try
            {
                await _serviceOrder.AddAsync(order);
                var result = await _serviceOrder.SaveChangesAsync();
                if (result > 0)
                {
                    HttpContext.Session.Remove("Cart");
                    return RedirectToAction("Thanks");
                }
            }
            catch (Exception)
            {
                TempData["Message"] = "Hata oluştu!";
            }

            return View(model);
        }
        public IActionResult Thanks()
        {
            return View();
        }
    }
}
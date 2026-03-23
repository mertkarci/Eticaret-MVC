using Eticaret.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Eticaret.WebUI.ExtensionMethods;
using Eticaret.Service.Abstract;
using Microsoft.Extensions.Logging;

namespace Eticaret.WebUI.Controllers
{
    public class FavouritesController : Controller
    {
        private readonly IService<Product> _service;
        private readonly ILogger<FavouritesController> _logger;

        public FavouritesController(IService<Product> service, ILogger<FavouritesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private string GetRoleName()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return User.IsInRole("Admin") ? "Admin" : "Üye";
            }
            return "Misafir";
        }

        [Route("favorilerim")]
        public ActionResult Index()
        {
            var favourites = GetFavourites();
            ViewData["IsFavouritesPage"] = true;
            return View(favourites);
        }

        private List<Product> GetFavourites()
        {
            return HttpContext.Session.GetJson<List<Product>>("GetFavourites") ?? [];


        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // Sayfada token olmasa bile 400 Bad Request vermesini engeller
        [Route("favorilerim/toggle")]
        public IActionResult Toggle(int productId)
        {
            var product = _service.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Ürün bulunamadı." });
            }

            var favourites = GetFavourites();
            var isCurrentlyFavorite = favourites.Any(p => p.Id == productId);
            bool isNowFavorite;

            if (isCurrentlyFavorite)
            {
                favourites.RemoveAll(i => i.Id == productId);
                isNowFavorite = false;
                
                _logger.LogInformation("Müşteri İşlemi: {User} adlı {Role} rolündeki kullanıcı '{ProductName}' (ID: {ProductId}) ürününü favorilerinden çıkardı.", User.Identity?.Name ?? "Ziyaretçi", GetRoleName(), product.Name, product.Id);
            }
            else
            {
                favourites.Add(product);
                isNowFavorite = true;
                
                _logger.LogInformation("Müşteri İşlemi: {User} adlı {Role} rolündeki kullanıcı '{ProductName}' (ID: {ProductId}) ürününü favorilerine ekledi.", User.Identity?.Name ?? "Ziyaretçi", GetRoleName(), product.Name, product.Id);
            }

            HttpContext.Session.SetJson("GetFavourites", favourites);

            return Json(new { success = true, isFavorite = isNowFavorite, count = favourites.Count });
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var favourites = GetFavourites();
            var productToRemove = favourites.FirstOrDefault(p => p.Id == productId);
            
            if (productToRemove != null)
            {
                favourites.RemoveAll(i => i.Id == productId);
                HttpContext.Session.SetJson("GetFavourites", favourites);
                
                _logger.LogInformation("Müşteri İşlemi: {User} adlı {Role} rolündeki kullanıcı '{ProductName}' (ID: {ProductId}) ürününü favorilerinden çıkardı.", User.Identity?.Name ?? "Ziyaretçi", GetRoleName(), productToRemove.Name, productToRemove.Id);
            }
            return RedirectToAction("Index");
        }
    }
}

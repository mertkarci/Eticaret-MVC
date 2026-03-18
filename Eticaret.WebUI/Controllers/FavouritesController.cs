using Eticaret.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Eticaret.WebUI.ExtensionMethods;
using Eticaret.Service.Abstract;

namespace Eticaret.WebUI.Controllers
{
    public class FavouritesController : Controller
    {
        private readonly IService<Product> _service;

        public FavouritesController(IService<Product> service)
        {
            _service = service;

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
            }
            else
            {
                favourites.Add(product);
                isNowFavorite = true;
            }

            HttpContext.Session.SetJson("GetFavourites", favourites);

            return Json(new { success = true, isFavorite = isNowFavorite, count = favourites.Count });
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var favourites = GetFavourites();
            if (favourites.Any(p => p.Id == productId))
            {
                favourites.RemoveAll(i => i.Id == productId);
                HttpContext.Session.SetJson("GetFavourites", favourites);
            }
            return RedirectToAction("Index");
        }
    }
}

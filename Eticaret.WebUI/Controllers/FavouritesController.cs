using Eticaret.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Eticaret.WebUI.ExtensionMethods;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Eticaret.Data;

namespace Eticaret.WebUI.Controllers
{
    public class FavouritesController : Controller
    {
        private readonly DatabaseContext _context;

        public FavouritesController(DatabaseContext context)
        {
            _context = context;
        }
        public ActionResult Index()
        {
            var favourites = GetFavourites();
            return View(favourites);
        }

        private List<Product> GetFavourites()
        {
            return HttpContext.Session.GetJson<List<Product>>("GetFavourites") ?? [];


        }
        public IActionResult Add(int productId)
        {
            var favourites = GetFavourites();
            var product = _context.Products.Find(productId);
            if (product != null && !favourites.Any(p => p.Id == productId))
            {
                favourites.Add(product);
                HttpContext.Session.SetJson("GetFavourites", favourites);

            }
            return RedirectToAction("Index");
        }
        public IActionResult Remove(int productId)
        {
            var favourites = GetFavourites();
            var product = _context.Products.Find(productId);
            if (product != null && favourites.Any(p => p.Id == productId))
            {
                favourites.RemoveAll(i => i.Id == productId);
                HttpContext.Session.SetJson("GetFavourites", favourites);

            }
            return RedirectToAction("Index");
        }

    }
}

using Eticaret.Data;
using Eticaret.WebUI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.WebUI.Controllers
{
    public class ProductsController : Controller
    {
        private readonly DatabaseContext _context;

        public ProductsController(DatabaseContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string q = "")
        {
            // Start with the base query
            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                // Convert search term to lowercase
                string query = q.ToLower();

                // Filter: Convert Database Name to lowercase AND compare with query
                products = products.Where(p => p.isActive && p.Name.ToLower().Contains(query));
            }
            else
            {
                // If no search, just get active products
                products = products.Where(p => p.isActive);
            }

            // Include related tables and execute query
            var result = await products
                                .Include(p => p.Brand)
                                .Include(p => p.Category)
                                .ToListAsync();

            return View(result);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            var model = new ProductDetailViewModel()
            {
                Product = product,
                RelatedProducts = _context.Products.Where(p => p.isActive && p.CategoryId == product.CategoryId && p.Id != product.Id).Take(4).ToList()
            };
            return View(model);
        }
    }
}

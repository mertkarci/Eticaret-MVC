using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Eticaret.Core.Entities;
using Eticaret.Data;
using Eticaret.WebUI.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Eticaret.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class ProductsController : Controller
    {
        private readonly DatabaseContext _context;

        public ProductsController(DatabaseContext context)
        {
            _context = context;
        }

        // AKILLI FİZİKSEL SİLME METODU (Dosya başka üründe yoksa siler)
        private async Task CheckAndDeletePhysicalFileAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            // Bu resim herhangi bir ürünün Kapağı olarak kullanılıyor mu?
            bool usedAsCover = await _context.Products.AnyAsync(p => p.Image == fileName);
            // Bu resim herhangi bir ürünün Galerisinde kullanılıyor mu?
            bool usedInGallery = await _context.ProductImages.AnyAsync(p => p.Name == fileName);

            // Eğer hiçbir yerde kullanılmıyorsa fiziksel dosyayı sil
            if (!usedAsCover && !usedInGallery)
            {
                FileHelper.FileRemover(fileName, "/wwwroot/img/products/");
            }
        }

        public async Task<IActionResult> Index()
        {
            var databaseContext = _context.Products.Include(p => p.Brand).Include(p => p.Category);
            return View(await databaseContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (product == null) return NotFound();

            return View(product);
        }

        public IActionResult Create()
        {
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? Image, List<IFormFile>? AdditionalImages)
        {
            ModelState.Remove("Brand");
            ModelState.Remove("Category");
            ModelState.Remove("ProductImages");
            ModelState.Remove("Slug");

            if (AdditionalImages != null && AdditionalImages.Count > 6)
            {
                ModelState.AddModelError("", "En fazla 6 adet galeri fotoğrafı ekleyebilirsiniz.");
            }

            if (ModelState.IsValid)
            {
                if (Image != null)
                {
                    product.Image = await FileHelper.FileLoaderAsync(Image, "/img/products/");
                }

                string baseSlug = UrlHelper.FriendlyUrl(product.Name);
                string finalSlug = baseSlug;
                int counter = 1;

                while (await _context.Products.AnyAsync(p => p.Slug == finalSlug))
                {
                    finalSlug = $"{baseSlug}-{counter++}";
                }

                product.Slug = finalSlug;

                _context.Add(product);
                await _context.SaveChangesAsync();

                if (AdditionalImages != null && AdditionalImages.Count > 0)
                {
                    var filesToUpload = AdditionalImages.Take(6).ToList();
                    foreach (var file in filesToUpload)
                    {
                        if (file.Length > 0)
                        {
                            string uploadedFileName = await FileHelper.FileLoaderAsync(file, "/img/products/");
                            var newProductImage = new ProductImage
                            {
                                ProductId = product.Id,
                                Name = uploadedFileName
                            };
                            _context.ProductImages.Add(newProductImage);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index), new { area = "Admin" });
            }

            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? Image, bool cbResmiSil, List<IFormFile>? AdditionalImages)
        {
            if (id != product.Id) return NotFound();

            ModelState.Remove("Brand");
            ModelState.Remove("Category");
            ModelState.Remove("ProductImages");
            ModelState.Remove("Slug");

            int currentImageCount = await _context.ProductImages.CountAsync(p => p.ProductId == id);
            int newImageCount = AdditionalImages?.Count ?? 0;

            if (currentImageCount + newImageCount > 6)
            {
                ModelState.AddModelError("", $"Mevcut fotoğraflarla birlikte en fazla 6 adet galeri fotoğrafı olabilir. Şu an {currentImageCount} adet kayıt var.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var dbProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
                    if (dbProduct == null) return NotFound();

                    dbProduct.Name = product.Name;
                    dbProduct.Description = product.Description;
                    dbProduct.Price = product.Price;
                    dbProduct.Stock = product.Stock;
                    dbProduct.BrandId = product.BrandId;
                    dbProduct.CategoryId = product.CategoryId;
                    dbProduct.isActive = product.isActive;
                    dbProduct.isHome = product.isHome;

                    string oldImage = dbProduct.Image;
                    bool coverChanged = false;

                    if (cbResmiSil)
                    {
                        dbProduct.Image = string.Empty;
                        coverChanged = true;
                    }
                    
                    if (Image != null)
                    {
                        dbProduct.Image = await FileHelper.FileLoaderAsync(Image, "/img/products/");
                        coverChanged = true;
                    }

                    string baseSlug = UrlHelper.FriendlyUrl(product.Name);
                    string finalSlug = baseSlug;
                    int counter = 1;

                    while (await _context.Products.AnyAsync(p => p.Slug == finalSlug && p.Id != id))
                    {
                        finalSlug = $"{baseSlug}-{counter++}";
                    }
                    dbProduct.Slug = finalSlug;

                    _context.Update(dbProduct);

                    if (AdditionalImages != null && AdditionalImages.Count > 0)
                    {
                        int allowedUploads = 6 - currentImageCount;
                        var filesToUpload = AdditionalImages.Take(allowedUploads).ToList();

                        foreach (var file in filesToUpload)
                        {
                            if (file.Length > 0)
                            {
                                string uploadedFileName = await FileHelper.FileLoaderAsync(file, "/img/products/");
                                var newProductImage = new ProductImage
                                {
                                    ProductId = dbProduct.Id,
                                    Name = uploadedFileName
                                };
                                _context.ProductImages.Add(newProductImage);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Kayıt başarılı olduktan SONRA eski kapağı güvenle sil
                    if (coverChanged && !string.IsNullOrEmpty(oldImage))
                    {
                        await CheckAndDeletePhysicalFileAsync(oldImage);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index), new { area = "Admin" });
            }

            product.ProductImages = await _context.ProductImages.Where(x => x.ProductId == id).ToListAsync();
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGalleryImage(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null) return Json(new { success = false, message = "Resim bulunamadı." });

            string fileName = image.Name;
            
            // 1. Önce Veritabanı bağını koparıyoruz
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            // 2. Sonra akıllı siliciye gönderiyoruz
            await CheckAndDeletePhysicalFileAsync(fileName);

            return Json(new { success = true });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                string coverImage = product.Image;
                var galleryImages = product.ProductImages?.Select(x => x.Name).ToList() ?? new List<string>();

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                // Tüm resimleri akıllı siliciyle kontrol et
                await CheckAndDeletePhysicalFileAsync(coverImage);
                foreach (var imgName in galleryImages)
                {
                    await CheckAndDeletePhysicalFileAsync(imgName);
                }
            }

            return RedirectToAction(nameof(Index), new { area = "Admin" });
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
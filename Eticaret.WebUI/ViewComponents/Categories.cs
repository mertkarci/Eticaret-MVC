using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Eticaret.WebUI;

public class Categories : ViewComponent
{
        private readonly ICategoryService _service;

        public Categories(ICategoryService service)
        {
            _service = service;

        }
        public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _service.GetTopMenuCategoriesAsync();
        return View(categories);
    }
}

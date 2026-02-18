using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Eticaret.WebUI;

public class Categories : ViewComponent
{
        private readonly IService<Category> _service;

        public Categories(IService<Category> service)
        {
            _service = service;

        }
        public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _service.GetAllAsync(c => c.isTopMenu && c.isActive);
        return View(categories);
    }
}

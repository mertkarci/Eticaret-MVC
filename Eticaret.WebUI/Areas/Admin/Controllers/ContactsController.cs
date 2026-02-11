using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Eticaret.Core.Entities;
using Eticaret.Data;

namespace Eticaret.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class ContactsController : Controller
    {
            private readonly DatabaseContext _context;
        public ContactsController(DatabaseContext context)
        {
            _context = context;
        }
       public IActionResult Index()
        {
            return View(_context.Contacts);
        }
    }
}

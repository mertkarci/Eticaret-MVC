using System.Diagnostics;
using System.Security.Claims;
using Eticaret.Core.Entities;
using Eticaret.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Eticaret.WebUI;

public class AccountsController : Controller
{
    private readonly DatabaseContext _context;

    public AccountsController(DatabaseContext context)
    {
        _context = context;
    }
    [Authorize]
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult SignIn()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> SignInAsync(LoginViewModel loginViewModel)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var account = await _context.AppUsers.FirstOrDefaultAsync(p => p.Email == loginViewModel.Email && p.Password == loginViewModel.Password && p.isActive);
                if (account == null)
                {
                    ModelState.AddModelError("", "Giriş Başarısız!");
                }
                else
                {
                    var claims = new List<Claim>()
                    {
                        new(ClaimTypes.Name,account.Name),
                        new(ClaimTypes.Email,account.Email),
                        new(ClaimTypes.Role, account.isAdmin ? "Admin" : "User"),
                        new("UserId", account.Id.ToString()),
                        new("UserGuid", account.UserGuid.ToString())
                    };
                    var userIdentity = new ClaimsIdentity(claims, "Login");
                    ClaimsPrincipal userPrincipal = new ClaimsPrincipal(userIdentity);
                    await HttpContext.SignInAsync(userPrincipal);
                    return RedirectToAction("Index", "Home");

                }
            }
            catch (Exception error)
            {
            Debug.WriteLine(error);
            ModelState.AddModelError("", "Hata!!");
        }
    }
        return View();
    }

    public IActionResult SignUp()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> SignUpAsync(AppUser appUser)
    {
        appUser.isAdmin = false;
        appUser.isActive = true;
        if (ModelState.IsValid)
        {
            await _context.AddAsync(appUser);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(appUser);

    }
}


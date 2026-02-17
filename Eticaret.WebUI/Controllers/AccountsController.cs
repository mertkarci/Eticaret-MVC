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
    [Authorize]
    public IActionResult Index()
    {
        // 1. Cookie'deki String Guid'i al
        var userGuidClaim = HttpContext.User.FindFirst("UserGuid");

        // Güvenlik: Cookie yoksa girişe at
        if (userGuidClaim == null) return RedirectToAction("SignIn");

        // 2. String'i C# GUID nesnesine çevir (Dönüştürme işlemi)
        if (!Guid.TryParse(userGuidClaim.Value, out Guid guidFromCookie))
        {
            // Eğer cookie bozuksa (guid formatında değilse) çıkış yap
            HttpContext.SignOutAsync();
            return RedirectToAction("SignIn");
        }

        // 3. ARTIK DOĞRU SORGULAMA YAPIYORUZ (Guid == Guid)
        // ToString() kullanmadan, direkt Guid nesnesi ile arıyoruz.
        AppUser user = _context.AppUsers.FirstOrDefault(p => p.UserGuid == guidFromCookie);

        // 4. Kullanıcı yine bulunamazsa (DB sıfırlandıysa vs.)
        if (user is null)
        {
            HttpContext.SignOutAsync();
            return RedirectToAction("SignIn");
        }
        var model = new UserEditViewModel()
        {
            Email = user.Email,
            Id = user.Id,
            Name = user.Name,
            Password = user.Password,
            Phone = user.Phone,
            Surname = user.Surname
        };

        return View(model);
    }

    [HttpPost, Authorize]
    public IActionResult Index(UserEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var userGuidClaim = HttpContext.User.FindFirst("UserGuid");

                // Güvenlik: Cookie yoksa girişe at
                if (userGuidClaim == null) return RedirectToAction("SignIn");

                // 2. String'i C# GUID nesnesine çevir (Dönüştürme işlemi)
                if (!Guid.TryParse(userGuidClaim.Value, out Guid guidFromCookie))
                {
                    // Eğer cookie bozuksa (guid formatında değilse) çıkış yap
                    HttpContext.SignOutAsync();
                    return RedirectToAction("SignIn");
                }
                AppUser user = _context.AppUsers.FirstOrDefault(p => p.UserGuid == guidFromCookie);
                if (user is not null)
                {
                    user.Name = model.Name;
                    user.Surname = model.Surname;
                    user.Email = model.Email;
                    user.Phone = model.Phone;
                    user.Password = model.Password;
                    _context.AppUsers.Update(user);
                    var result = _context.SaveChanges();

                    if (result > 0)
                    {
                        TempData["Message"] = @"<div class='alert alert-success alert-dismissible fade show rounded-0' role='alert'>
    <strong> Tebrikler! </strong>
    <button type='button' class='btn-close' data-bs-dismiss='alert' aria-label='Close'></button>
</div>";

                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Hata oluştu!");
            }
        }
        return View(model);
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
                        new("UserGuid", account.UserGuid.ToString()),
                        new("ReturnUrl", loginViewModel.ReturnUrl ?? "/")
                    };
                    var userIdentity = new ClaimsIdentity(claims, "Login");
                    ClaimsPrincipal userPrincipal = new ClaimsPrincipal(userIdentity);
                    await HttpContext.SignInAsync(userPrincipal);
                    return Redirect(string.IsNullOrEmpty(loginViewModel.ReturnUrl) ? "/" :
                    loginViewModel.ReturnUrl);

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
    public async Task<IActionResult> SignOutAsync()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("SignIn");
    }
}


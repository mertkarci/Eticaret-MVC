using System.Diagnostics;
using System.Security.Claims;
using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Eticaret.WebUI.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.WebUI;

public class AccountsController : Controller
{
    private readonly IService<AppUser> _service;
    private readonly IService<Order> _serviceOrder;

    public AccountsController(IService<AppUser> service, IService<Order> serviceOrder)
    {
        _service = service;
        _serviceOrder = serviceOrder;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        // 1. Cookie'deki String Guid'i al
        var userGuidClaim = HttpContext.User.FindFirst("UserGuid");

        if (userGuidClaim == null) return RedirectToAction("SignIn");

        // 2. String'i C# GUID nesnesine çevir (Güvenlik ve Performans için)
        if (!Guid.TryParse(userGuidClaim.Value, out Guid guidFromCookie))
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("SignIn");
        }

        // 3. SERVICE İLE ÇAĞIRMA (Async)
        AppUser user = await _service.GetAsync(p => p.UserGuid == guidFromCookie);

        // 4. Kullanıcı kontrolü
        if (user is null)
        {
            await HttpContext.SignOutAsync();
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

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Index(UserEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // GUID ALMA VE GÜVENLİK
                var userGuidClaim = HttpContext.User.FindFirst("UserGuid");
                if (userGuidClaim == null) return RedirectToAction("SignIn");

                if (!Guid.TryParse(userGuidClaim.Value, out Guid guidFromCookie))
                {
                    await HttpContext.SignOutAsync();
                    return RedirectToAction("SignIn");
                }

                // VERİYİ SERVİSTEN ÇEKME
                AppUser user = await _service.GetAsync(p => p.UserGuid == guidFromCookie);

                if (user is not null)
                {
                    // Verileri Güncelle
                    user.Name = model.Name;
                    user.Surname = model.Surname;
                    user.Email = model.Email;
                    user.Phone = model.Phone;

                    // Şifre boş gelirse eski şifreyi koru
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        user.Password = model.Password;
                    }

                    // GÜNCELLEME VE KAYDETME
                    _service.Update(user);
                    var result = await _service.SaveChangesAsync();

                    if (result > 0)
                    {
                        TempData["Message"] = @"<div class='alert alert-success alert-dismissible fade show rounded-0' role='alert'>
                            <strong> Tebrikler! </strong> Bilgileriniz başarıyla güncellendi.
                            <button type='button' class='btn-close' data-bs-dismiss='alert' aria-label='Close'></button>
                        </div>";

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["Message"] = @"<div class='alert alert-info alert-dismissible fade show rounded-0' role='alert'>
                            Değişiklik yapılmadı.
                            <button type='button' class='btn-close' data-bs-dismiss='alert' aria-label='Close'></button>
                        </div>";
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Güncelleme sırasında bir hata oluştu!");
                Debug.WriteLine(ex);
            }
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult SignIn(string returnUrl = null)
    {
        // Yönlendirme Kontrolü ve ViewBag Doldurma İşlemi
        if (!string.IsNullOrEmpty(returnUrl))
        {
            if (returnUrl.Contains("/Checkout", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.AlertMessage = "Siparişinizi tamamlayabilmek için giriş yapmanız veya hesap oluşturmanız gerekmektedir.";
                ViewBag.AlertType = "info";
                ViewBag.AlertIcon = "bi-bag-check-fill";
            }
            else if (returnUrl.Contains("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.AlertMessage = "Yönetim paneline erişmek için yetkili bir hesapla giriş yapmalısınız.";
                ViewBag.AlertType = "danger";
                ViewBag.AlertIcon = "bi-shield-lock-fill";
            }
            else
            {
                ViewBag.AlertMessage = "Görüntülemeye çalıştığınız sayfaya erişmek için lütfen giriş yapın.";
                ViewBag.AlertType = "warning";
                ViewBag.AlertIcon = "bi-exclamation-triangle-fill";
            }
        }

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SignInAsync(LoginViewModel loginViewModel)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var account = await _service.GetAsync(p => p.Email == loginViewModel.Email && p.Password == loginViewModel.Password && p.isActive);
                if (account == null)
                {
                    ModelState.AddModelError("", "Giriş Başarısız!");
                }
                else
                {
                    var claims = new List<Claim>()
                    {
                        new(ClaimTypes.Name, account.Name),
                        new(ClaimTypes.Email, account.Email),
                        new(ClaimTypes.Role, account.isAdmin ? "Admin" : "User"),
                        new("UserId", account.Id.ToString()),
                        new("UserGuid", account.UserGuid.ToString()),
                        new("ReturnUrl", loginViewModel.ReturnUrl ?? "/")
                    };

                    var userIdentity = new ClaimsIdentity(claims, "Login");
                    ClaimsPrincipal userPrincipal = new ClaimsPrincipal(userIdentity);
                    await HttpContext.SignInAsync(userPrincipal);

                    return Redirect(string.IsNullOrEmpty(loginViewModel.ReturnUrl) ? "/" : loginViewModel.ReturnUrl);
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
        appUser.isAdmin = false; // başlangıçta nolursa olsun admin olmasın
        appUser.isActive = true;
        if (ModelState.IsValid)
        {
            await _service.AddAsync(appUser);
            await _service.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(appUser);
    }

    public async Task<IActionResult> SignOutAsync()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("SignIn");
    }

    [Authorize]
    public async Task<IActionResult> MyOrders()
    {
        var userGuidClaim = HttpContext.User.FindFirst("UserGuid");

        if (userGuidClaim == null) return RedirectToAction("SignIn");

        if (!Guid.TryParse(userGuidClaim.Value, out Guid guidFromCookie))
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("SignIn");
        }

        AppUser user = await _service.GetAsync(p => p.UserGuid == guidFromCookie);

        if (user is null)
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("SignIn");
        }

        // Eager Loading (Include ve ThenInclude) ile Siparişleri Çekme
        var model = await _serviceOrder.GetQueryable()
            .Where(p => p.AppUserId == user.Id)
            .Include(p => p.OrderLines)
                .ThenInclude(p => p.Product)
            .ToListAsync();

        return View(model);
    }

    [HttpGet]
    public IActionResult PasswordRenew()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> PasswordRenew(string Email)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError("", "Email alanı boş olamaz!");
            return View();
        }

        AppUser user = await _service.GetAsync(p => p.Email == Email);
        if (user is null)
        {
            ModelState.AddModelError("", "Geçersiz bir email girdiniz.");
            return View();
        }

        string message = $"Şifrenizi bağlantıyı kullanarak sıfırlayınız: <a href='http://localhost:5292/Accounts/PasswordChange?user={user.UserGuid.ToString()}'>Buraya tıklayınız</a>";
        var result = await MailHelper.SendmMailAsync(Email, "Şifreyi Yenile", message);

        if (result)
        {
            TempData["Message"] = "Şifre sıfırlama bağlantınız mail adresinize başarıyla gönderilmiştir.";
        }
        else
        {
            TempData["Message"] = "Mail gönderilirken bir hata oluştu!";
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> PasswordChange(string user)
    {
        if (string.IsNullOrEmpty(user))
        {
            return BadRequest("Geçersiz istek!");
        }

        // Güvenlik ve Dönüşüm: String to Guid
        if (!Guid.TryParse(user, out Guid parsedGuid))
        {
            return BadRequest("Geçersiz link formatı!");
        }

        AppUser appUser = await _service.GetAsync(p => p.UserGuid == parsedGuid);

        if (appUser is null)
        {
            return NotFound("Geçersiz veya süresi dolmuş değer.");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> PasswordChange(string user, string password)
    {
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
        {
            return BadRequest("Lütfen tüm alanları doldurun!");
        }

        if (!Guid.TryParse(user, out Guid parsedGuid))
        {
            return BadRequest("Geçersiz link formatı!");
        }

        AppUser appUser = await _service.GetAsync(p => p.UserGuid == parsedGuid);

        if (appUser is null)
        {
            ModelState.AddModelError("", "Kullanıcı bulunamadı.");
            return View();
        }

        appUser.Password = password;

        // Servis Update
        _service.Update(appUser);
        var result = await _service.SaveChangesAsync();

        if (result > 0)
        {
            TempData["Message"] = @"<div class='alert alert-success alert-dismissible fade show rounded-0' role='alert'>
                            Şifreniz başarıyla güncellenmiştir! Lütfen yeni şifrenizle giriş yapın.
                            <button type='button' class='btn-close' data-bs-dismiss='alert' aria-label='Close'></button>
                        </div>";

            return RedirectToAction("SignIn");
        }
        else
        {
            ModelState.AddModelError("", "Güncelleme başarısız veya aynı şifreyi girdiniz!");
        }

        return View();
    }
    public async Task<IActionResult> MyOrderDetails(int? id)
    {
        // 1. Cookie'deki String Guid'i al
        var userGuidClaim = HttpContext.User.FindFirst("UserGuid");

        if (userGuidClaim == null) return RedirectToAction("SignIn");

        // 2. String'i C# GUID nesnesine çevir (Güvenlik ve Performans için)
        if (!Guid.TryParse(userGuidClaim.Value, out Guid guidFromCookie))
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("SignIn");
        }

        AppUser user = await _service.GetAsync(p => p.UserGuid == guidFromCookie);

        if (user is null)
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("SignIn");
        }

        var order = await _serviceOrder.GetQueryable().Include(x => x.OrderLines).ThenInclude(x => x.Product)
        .FirstOrDefaultAsync(x => x.Id == id && x.AppUserId == user.Id);
        if (order == null)
        {
            return NotFound("Sipariş bulunamadı!");
        }
        return View(order);

    }


}

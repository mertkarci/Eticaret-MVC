﻿using System.Diagnostics;
using System.Security.Claims;
using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Eticaret.WebUI.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt;
using BCrypt.Net;

namespace Eticaret.WebUI;

[Route("hesabim")]
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
    [HttpGet("")]
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


        AppUser user = await _service.GetAsync(p => p.UserGuid == guidFromCookie);

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

    [HttpPost("")]
    [Authorize]
    public async Task<IActionResult> Index(UserEditViewModel model)
    {

        if (!string.IsNullOrWhiteSpace(model.Phone))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Phone, @"^05[0-9]{9}$"))
            {
                ModelState.AddModelError("Phone", "Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır.");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                var userGuidClaim = HttpContext.User.FindFirst("UserGuid");
                if (userGuidClaim == null || !Guid.TryParse(userGuidClaim.Value, out Guid guidFromCookie))
                {
                    await HttpContext.SignOutAsync();
                    return RedirectToAction("SignIn");
                }

                AppUser user = await _service.GetAsync(p => p.UserGuid == guidFromCookie);

                if (user is not null)
                {

                    user.Name = model.Name;
                    user.Surname = model.Surname;

                    // if(model.Phone != user.Phone) { SendSmsAndRedirectToVerification(...) }

                    if (user.Phone != model.Phone && await _service.GetAsync(u => u.Phone == model.Phone) != null)
                    {
                        ModelState.AddModelError("Phone", "Bu telefon numarası zaten kullanılıyor.");
                        return View(model);
                    }
                    
                    user.Phone = model.Phone;

                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    }

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

        var currentUser = await _service.GetAsync(p => p.UserGuid.ToString() == HttpContext.User.FindFirst("UserGuid").Value);
        if (currentUser != null) model.Email = currentUser.Email;

        return View(model);
    }

    [HttpGet("giris-yap")]
    public IActionResult SignIn(string returnUrl = null)
    {
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

    [HttpPost("giris-yap")]
    public async Task<IActionResult> SignInAsync(LoginViewModel loginViewModel)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var account = await _service.GetAsync(p => p.Email == loginViewModel.Email && p.isActive);
                if (account == null)
                {
                    ModelState.AddModelError("", "Giriş Başarısız!");
                }
                else if (!BCrypt.Net.BCrypt.Verify(loginViewModel.Password, account.Password))
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
    [HttpGet("kayit-ol")]
    public IActionResult SignUp()
    {
        return View();
    }

    [HttpPost("kayit-ol")]
    public async Task<IActionResult> SignUpAsync(AppUser appUser)
    {
        appUser.isAdmin = false;
        appUser.isActive = true;

        if (!string.IsNullOrWhiteSpace(appUser.Phone))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(appUser.Phone, @"^05[0-9]{9}$"))
            {
                ModelState.AddModelError("Phone", "Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır.");
            }
        }

        if (string.IsNullOrWhiteSpace(appUser.Password))
        {
            ModelState.AddModelError("Password", "Şifre alanı boş geçilemez.");
        }
        appUser.Password = BCrypt.Net.BCrypt.HashPassword(appUser.Password);
        if (ModelState.IsValid)
        {
            if (await _service.GetAsync(u => u.Email == appUser.Email) != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                return View(appUser);
            }
            if (await _service.GetAsync(u => u.Phone == appUser.Phone) != null)
            {
                ModelState.AddModelError("Phone", "Bu telefon numarası zaten kullanılıyor.");
                return View(appUser);
            }
            await _service.AddAsync(appUser);
            await _service.SaveChangesAsync();
            TempData["Message"] = "Kaydınız başarıyla oluşturuldu. Lütfen giriş yapınız.";
            return RedirectToAction(nameof(SignIn));
        }

        return View(appUser);
    }
    [HttpGet("cikis-yap")]
    public async Task<IActionResult> SignOutAsync()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("SignIn");
    }

    [Authorize]
    [HttpGet("siparislerim")]
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

    [HttpGet("sifremi-unuttum")]
    public IActionResult PasswordRenew()
    {
        return View();
    }
    [HttpPost("sifremi-unuttum")]
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

        string resetToken = Guid.NewGuid().ToString();

        user.PasswordResetToken = resetToken;
        user.ResetTokenExpires = DateTime.Now.AddHours(2);
        _service.Update(user);
        await _service.SaveChangesAsync();

        string resetLink = Url.Action("PasswordChange", "Accounts", new { token = resetToken }, Request.Scheme);

        string message = $@"
        <div style='font-family: Arial, sans-serif; padding: 20px;'>
            <h3>Şifre Sıfırlama Talebi</h3>
            <p>Merhaba {user.Name},</p>
            <p>Şifrenizi yenilemek için aşağıdaki bağlantıya tıklayabilirsiniz. Bu bağlantı sadece size özeldir.</p>
            <p><a href='{resetLink}' style='display: inline-block; padding: 10px 20px; background-color: #0d6efd; color: white; text-decoration: none; border-radius: 5px;'>Şifremi Yenile</a></p>
            <p style='font-size: 12px; color: #6c757d; margin-top: 20px;'>Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
        </div>";

        var result = await MailHelper.SendmMailAsync(Email, message, "Şifremi Yenile");

        if (result)
        {
            TempData["Message"] = @"<div class='alert alert-success'>Şifre sıfırlama bağlantınız mail adresinize başarıyla gönderilmiştir. Lütfen gelen kutunuzu (ve Spam klasörünü) kontrol edin.</div>";
        }
        else
        {
            TempData["Message"] = @"<div class='alert alert-danger'>Mail gönderilirken sistemsel bir hata oluştu! Lütfen daha sonra tekrar deneyin.</div>";

        }

        return View();
    }

    [HttpGet("sifremi-yenile")]
    public async Task<IActionResult> PasswordChange(string token) 
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Geçersiz istek!");
        }


        AppUser appUser = await _service.GetAsync(p => p.PasswordResetToken == token);

        if (appUser is null || appUser.ResetTokenExpires < DateTime.Now)
        {
            return NotFound("Geçersiz veya süresi dolmuş bir şifre sıfırlama bağlantısı kullandınız.");
        }

        ViewBag.Token = token;

        return View();
    }

    [HttpPost("sifremi-yenile")]
    public async Task<IActionResult> PasswordChange(string token, string password) 
    {
        ViewBag.Token = token;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Lütfen tüm alanları doldurun!");
            return View();
        }

        if (password.Length < 6)
        {
            ModelState.AddModelError("", "Şifreniz güvenlik gereği en az 6 karakter uzunluğunda olmalıdır.");
            return View();
        }

        if (password.Contains(" "))
        {
            ModelState.AddModelError("", "Şifreniz boşluk karakteri içeremez.");
            return View();
        }

        AppUser appUser = await _service.GetAsync(p => p.PasswordResetToken == token);

        if (appUser is null || appUser.ResetTokenExpires < DateTime.Now)
        {
            ModelState.AddModelError("", "Geçersiz veya süresi dolmuş bir şifre sıfırlama bağlantısı kullandınız.");
            return View();
        }

        appUser.Password = BCrypt.Net.BCrypt.HashPassword(password);
        appUser.PasswordResetToken = null; 
        appUser.ResetTokenExpires = null;  

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
    [HttpGet("siparislerim/{orderNumber}")]
    public async Task<IActionResult> MyOrderDetails(string orderNumber)
    {
        if (string.IsNullOrEmpty(orderNumber)) return NotFound();

        var userGuidClaim = HttpContext.User.FindFirst("UserGuid");
        if (userGuidClaim == null || !Guid.TryParse(userGuidClaim.Value, out Guid guidFromCookie))
        {
            return RedirectToAction("SignIn");
        }

        AppUser user = await _service.GetAsync(p => p.UserGuid == guidFromCookie);
        if (user is null) return RedirectToAction("SignIn");

        var order = await _serviceOrder.GetQueryable()
            .Include(x => x.OrderLines)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.OrderNumber == orderNumber && x.AppUserId == user.Id);

        if (order == null)
        {
            return NotFound("Sipariş bulunamadı!");
        }

        return View(order);
    }


}

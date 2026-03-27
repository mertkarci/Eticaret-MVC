﻿﻿﻿﻿﻿using System.Diagnostics;
using System.Security.Claims;
using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Eticaret.WebUI.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
namespace Eticaret.WebUI;

[Route("hesabim")]
public class AccountsController : Controller
{
    private readonly IService<AppUser> _service;
    private readonly IService<Order> _serviceOrder;
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ILogger<AccountsController> _logger;
    private readonly IService<Comment> _serviceComment;

    public AccountsController(IService<AppUser> service, IService<Order> serviceOrder, IAuthService authService, IUserService userService, ILogger<AccountsController> logger, IService<Comment> serviceComment)
    {
        _service = service;
        _serviceOrder = serviceOrder;
        _authService = authService;
        _userService = userService;
        _logger = logger;
        _serviceComment = serviceComment;
    }

    private string GetRoleName()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return User.IsInRole("Admin") ? "Admin" : "Üye";
        }
        return "Misafir";
    }



    [Authorize]
    [HttpGet("")]
    public async Task<IActionResult> Index()
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
            var userGuidClaim = HttpContext.User.FindFirst("UserGuid");
            if (userGuidClaim == null || !Guid.TryParse(userGuidClaim.Value, out Guid guidFromCookie))
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("SignIn");
            }

            var result = await _userService.EditAccount(model.Phone, model.Name, model.Surname, guidFromCookie);

            if (result.IsSuccess)
            {
                TempData["Message"] = @"<div class='alert alert-success alert-dismissible fade show rounded-0' role='alert'>
                <strong> Tebrikler! </strong> Bilgileriniz başarıyla güncellendi.
                <button type='button' class='btn-close' data-bs-dismiss='alert' aria-label='Close'></button>
            </div>";
            
                _logger.LogInformation("Müşteri İşlemi: {User} adlı {Role} rolündeki kullanıcı profil bilgilerini güncelledi.", model.Email ?? User.Identity?.Name, GetRoleName());
                
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError(result.ErrorMessage.Contains("telefon") ? "Phone" : "", result.ErrorMessage);
            }
        }

        var currentUser = await _service.GetAsync(p => p.UserGuid.ToString() == HttpContext.User.FindFirst("UserGuid").Value);
        if (currentUser != null) model.Email = currentUser.Email;

        return View(model);
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

        var model = await _serviceOrder.GetQueryable()
            .Where(p => p.AppUserId == user.Id)
            .Include(p => p.OrderLines)
                .ThenInclude(p => p.Product)
            .ToListAsync();

        return View(model);
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

        if (order == null) return NotFound("Sipariş bulunamadı!");

        var reviewedProductIds = await _serviceComment.GetQueryable()
            .Where(c => c.AppUserId == user.Id) // Sipariş bazlı değil, KULLANICI bazlı kontrol
            .Select(c => c.ProductId)
            .ToListAsync();

        var pendingReviewsCount = order.OrderLines
            .Where(ol => !reviewedProductIds.Contains(ol.ProductId))
            .DistinctBy(ol => ol.ProductId)
            .Count();

        ViewBag.HasPendingReviews = pendingReviewsCount > 0;

        return View(order);
    }

    [Authorize]
    [HttpGet("degerlendirmelerim")]
    public async Task<IActionResult> MyReviews()
    {
        var userGuidClaim = HttpContext.User.FindFirst("UserGuid");
        if (userGuidClaim == null || !Guid.TryParse(userGuidClaim.Value, out Guid guidFromCookie))
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

        var comments = await _serviceComment.GetQueryable()
            .Include(c => c.Product)
            .Where(c => c.AppUserId == user.Id)
            .OrderByDescending(c => c.CreateDate)
            .ToListAsync();

        return View(comments);
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

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("giris-yap")]
    [EnableRateLimiting("AuthLimit")]
    public async Task<IActionResult> SignInAsync(LoginViewModel loginViewModel)
    {
        if (!ModelState.IsValid) return View(loginViewModel);

        var result = await _authService.LoginAsync(loginViewModel.Email, loginViewModel.Password, loginViewModel.ReturnUrl);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.ErrorMessage);
            return View(loginViewModel);
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal);
        
        var role = result.Principal.IsInRole("Admin") ? "Admin" : "Üye";
        _logger.LogInformation("Müşteri İşlemi: {User} adlı {Role} rolündeki kullanıcı sisteme giriş yaptı.", loginViewModel.Email, role);
        
        return Redirect(string.IsNullOrEmpty(loginViewModel.ReturnUrl) ? "/" : loginViewModel.ReturnUrl);
    }

    [HttpGet("kayit-ol")]
    public IActionResult SignUp() => View();

    [HttpPost("kayit-ol")]
    [EnableRateLimiting("AuthLimit")]
    public async Task<IActionResult> SignUpAsync(AppUser appUser)
    {
        if (!string.IsNullOrWhiteSpace(appUser.Phone) && !System.Text.RegularExpressions.Regex.IsMatch(appUser.Phone, @"^05[0-9]{9}$"))
            ModelState.AddModelError("Phone", "Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır.");

        if (string.IsNullOrWhiteSpace(appUser.Password))
            ModelState.AddModelError("Password", "Şifre alanı boş geçilemez.");

        if (!ModelState.IsValid) return View(appUser);

        var result = await _authService.RegisterAsync(appUser.Name, appUser.Surname, appUser.Email, appUser.Phone, appUser.Password);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.ErrorMessage);
            return View(appUser);
        }

        TempData["Message"] = "Kaydınız başarıyla oluşturuldu. Lütfen giriş yapınız.";
        _logger.LogInformation("Müşteri İşlemi: {User} adlı yeni kullanıcı sisteme Üye olarak kayıt oldu.", appUser.Email);
        
        return RedirectToAction(nameof(SignIn));
    }

    [HttpGet("cikis-yap")]
    public async Task<IActionResult> SignOutAsync()
    {
        var roleName = GetRoleName();
        var userName = HttpContext.User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
        
        await HttpContext.SignOutAsync();
        _logger.LogInformation("Müşteri İşlemi: {User} adlı {Role} rolündeki kullanıcı sistemden çıkış yaptı.", userName, roleName);
        return RedirectToAction("SignIn");
    }

    [HttpPost("google-ile-giris")]
    [ValidateAntiForgeryToken]
    public IActionResult GoogleLogin(string returnUrl = "/")
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse", "Accounts", new { returnUrl }) };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-donus")]
    public async Task<IActionResult> GoogleResponse(string returnUrl = "/")
    {
        var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authResult.Succeeded || authResult.Principal == null)
            return RedirectToAction("SignIn");

        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
        var name = authResult.Principal.FindFirstValue(ClaimTypes.Name);
        var surname = authResult.Principal.FindFirstValue(ClaimTypes.Surname);

        if (string.IsNullOrEmpty(email)) return RedirectToAction("SignIn");

        var result = await _authService.GoogleLoginAsync(email, name, surname, returnUrl);

        if (!result.IsSuccess)
        {
            TempData["Message"] = result.ErrorMessage;
            return RedirectToAction("SignIn");
        }

        await HttpContext.SignOutAsync();
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal);

        var role = result.Principal.IsInRole("Admin") ? "Admin" : "Üye";
        _logger.LogInformation("Müşteri İşlemi: {User} adlı {Role} rolündeki kullanıcı Google hesabı ile sisteme giriş yaptı.", email, role);

        return Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }

    [HttpGet("sifremi-unuttum")]
    public IActionResult PasswordRenew() => View();

    [HttpPost("sifremi-unuttum")]
    [EnableRateLimiting("AuthLimit")]
    public async Task<IActionResult> PasswordRenew(string Email)
    {
        if (string.IsNullOrWhiteSpace(Email)) { ModelState.AddModelError("", "Email alanı boş olamaz!"); return View(); }

        var result = await _authService.GeneratePasswordResetTokenAsync(Email);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.ErrorMessage);
            return View();
        }

        string resetLink = Url.Action("PasswordChange", "Accounts", new { token = result.ResetToken }, Request.Scheme);
        string message = $@"
        <div style='font-family: Arial, sans-serif; padding: 20px;'>
            <h3>Şifre Sıfırlama Talebi</h3>
            <p>Merhaba {result.UserName},</p>
            <p>Şifrenizi yenilemek için aşağıdaki bağlantıya tıklayabilirsiniz. Bu bağlantı sadece size özeldir.</p>
            <p><a href='{resetLink}' style='display: inline-block; padding: 10px 20px; background-color: #0d6efd; color: white; text-decoration: none; border-radius: 5px;'>Şifremi Yenile</a></p>
            <p style='font-size: 12px; color: #6c757d; margin-top: 20px;'>Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
        </div>";

        var mailSent = await MailHelper.SendmMailAsync(Email, message, "Şifremi Yenile");

        if (mailSent) TempData["Message"] = "<div class='alert alert-success'>Bağlantı mail adresinize gönderildi.</div>";
        else TempData["Message"] = "<div class='alert alert-danger'>Sistemsel bir hata oluştu.</div>";
        
        _logger.LogInformation("Müşteri İşlemi: {User} adlı {Role} rolündeki kullanıcı şifre sıfırlama talebinde bulundu.", Email, GetRoleName());

        return View();
    }

    [HttpGet("sifremi-yenile")]
    public IActionResult PasswordChange(string token)
    {
        if (string.IsNullOrEmpty(token)) return BadRequest("Geçersiz istek!");
        ViewBag.Token = token;
        return View();
    }

    [HttpPost("sifremi-yenile")]
    [EnableRateLimiting("AuthLimit")]
    public async Task<IActionResult> PasswordChange(string token, string password)
    {
        ViewBag.Token = token;
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(password)) { ModelState.AddModelError("", "Tüm alanları doldurun!"); return View(); }
        if (password.Length < 6) { ModelState.AddModelError("", "Şifreniz en az 6 karakter olmalıdır."); return View(); }

        var result = await _authService.ResetPasswordAsync(token, password);

        if (result.IsSuccess)
        {
            TempData["Message"] = "<div class='alert alert-success alert-dismissible fade show rounded-0' role='alert'>Şifreniz başarıyla güncellenmiştir! Lütfen yeni şifrenizle giriş yapın.<button type='button' class='btn-close' data-bs-dismiss='alert'></button></div>";
            _logger.LogInformation("Müşteri İşlemi: Bir {Role} başarıyla şifresini yeniledi.", GetRoleName());
            
            return RedirectToAction("SignIn");
        }

        ModelState.AddModelError("", result.ErrorMessage);
        return View();
    }
}
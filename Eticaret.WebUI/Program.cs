using System.Security.Claims;
using Eticaret.Data;
using Eticaret.Service.Abstract;
using Eticaret.Service.Concrete;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore; // UseSqlite için gerekli

var builder = WebApplication.CreateBuilder(args);

var webUiPath = builder.Environment.ContentRootPath;

// Bir üst klasöre çıkıp (..) "Eticaret.Data" klasörünü hedefliyoruz.
// DİKKAT: Klasör adın "Eticaret.Data" değilse burayı kendi klasör adınla değiştir.
var dbPath = Path.GetFullPath(Path.Combine(webUiPath, "..", "Eticaret.Data", "Eticaret.db"));

// Konsola yazdıralım ki doğru yeri bulduğundan emin ol
Console.WriteLine($"--------------------------------------------------");
Console.WriteLine($"KULLANILAN DB YOLU: {dbPath}");
Console.WriteLine($"--------------------------------------------------");

// DbContext'e bu özel yolu veriyoruz.
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
// --------------------------------------------------

// Servisleri ekliyoruz
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// --- 1. GÜVENLİK GÜNCELLEMESİ: SESSION (OTURUM) AYARLARI ---
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Eticaret.Session";
    options.Cookie.HttpOnly = true; // JS ile okunmasını engeller (XSS koruması)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Sadece HTTPS üzerinden iletilir
    options.Cookie.SameSite = SameSiteMode.Strict; // CSRF koruması
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.IOTimeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddScoped(typeof(IService<>), typeof(Service<>));
builder.Services.AddScoped<IOrderService, OrderService>();

// --- 2. GÜVENLİK GÜNCELLEMESİ: AUTHENTICATION (GİRİŞ) AYARLARI ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(p =>
{
    p.LoginPath = "/hesabim/giris-yap";
    p.AccessDeniedPath = "/404";

    p.Cookie.Name = "Account";
    p.Cookie.HttpOnly = true; // KESİNLİKLE OLMALI: JS ile çerez hırsızlığını engeller
    p.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Sadece HTTPS
    p.Cookie.SameSite = SameSiteMode.Strict; // Dış sitelerden gelen sahte POST isteklerini reddeder
    p.Cookie.IsEssential = true;

    // Güvenli oturum süresi yönetimi (MaxAge yerine)
    p.ExpireTimeSpan = TimeSpan.FromDays(1);
    p.SlidingExpiration = true; // Kullanıcı aktifse süreyi otomatik uzatır
});

// Authorization (Yetki) Ayarları
builder.Services.AddAuthorization(p =>
{
    p.AddPolicy("AdminPolicy", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    p.AddPolicy("UserPolicy", policy => policy.RequireClaim(ClaimTypes.Role, "Admin", "User"));
});

var app = builder.Build();

// Dil ve Para Birimi Ayarları
var supportedCultures = new[] { new System.Globalization.CultureInfo("tr-TR") };
supportedCultures[0].NumberFormat.CurrencySymbol = "₺";
supportedCultures[0].NumberFormat.CurrencyPositivePattern = 1;

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Hata Yönetimi
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Statik Dosyalar (CSS, JS, Resimler için ŞART)
app.UseStaticFiles();

app.UseRouting();

// MİMARİ SIRA ÇOK ÖNEMLİDİR (Dokunmadık, mükemmel ayarlamışsın)
app.UseSession();
app.UseAuthentication(); // Önce kimlik doğrulama
app.UseAuthorization();  // Sonra yetkilendirme

app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Main}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

using System.Security.Claims;
using Eticaret.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore; // UseSqlite için gerekli

var builder = WebApplication.CreateBuilder(args);

// --- 1. VERİTABANI YOLU AYARI (KRİTİK KISIM) ---
// Mevcut WebUI klasörünün yolunu alıyoruz
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
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Eticaret.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.IOTimeout = TimeSpan.FromMinutes(10);
});


;

// Authentication (Giriş) Ayarları
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie
(p =>
{
    p.LoginPath = "/Accounts/SignIn";
    p.AccessDeniedPath = "/AccessDenied";
    p.Cookie.Name = "Account";
    p.Cookie.MaxAge = TimeSpan.FromDays(1);
    p.Cookie.IsEssential = true;
});

// Authorization (Yetki) Ayarları
builder.Services.AddAuthorization(p =>
{
    p.AddPolicy("AdminPolicy", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    p.AddPolicy("UserPolicy", policy => policy.RequireClaim(ClaimTypes.Role, "Admin", "User", "Customer"));
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
app.UseSession();

app.UseAuthentication(); // Önce kimlik doğrulama
app.UseAuthorization();  // Sonra yetkilendirme

// Rotalar
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Main}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
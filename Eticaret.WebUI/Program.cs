using System.Security.Claims;
using System.Threading.RateLimiting;
using Eticaret.Data;
using Eticaret.Service.Abstract;
using Eticaret.Service.Concrete;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var webUiPath = builder.Environment.ContentRootPath;

builder.Host.UseSerilog((context, config) =>
{
    var logDbPath = Path.GetFullPath(Path.Combine(webUiPath, "..", "Eticaret.Data", "Logs.db"));

    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.Console()
        .WriteTo.SQLite(
            sqliteDbPath: logDbPath,
            tableName: "AppLogs",
            storeTimestampInUtc: true // Tabloyu otomatik oluşturması için şart
        );
});

var dbPath = Path.GetFullPath(Path.Combine(webUiPath, "..", "Eticaret.Data", "Eticaret.db"));

var logDbPath = Path.GetFullPath(Path.Combine(webUiPath, "..", "Eticaret.Data", "Logs.db"));

Console.WriteLine($"--------------------------------------------------");
Console.WriteLine($"ANA DB: {dbPath}");
Console.WriteLine($"LOG DB: {logDbPath}");
Console.WriteLine($"--------------------------------------------------");

builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("AuthLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.AddTokenBucketLimiter("ProductLimit", limiterOptions =>
    {
        limiterOptions.TokenLimit = 30;

        limiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(10);

        limiterOptions.TokensPerPeriod = 10;

        limiterOptions.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("CheckoutLimit", limiterOptions =>
        {
            limiterOptions.PermitLimit = 3;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });
    options.AddFixedWindowLimiter("ContactLimit", limiterOptions =>
        {
            limiterOptions.PermitLimit = 3;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });

    // options.AddFixedWindowLimiter("CouponLimit", limiterOptions =>
    // {
    //     limiterOptions.PermitLimit = 5; // 1 dakikada maksimum 5 kupon denemesi
    //     limiterOptions.Window = TimeSpan.FromMinutes(1);
    //     limiterOptions.QueueLimit = 0;
    // });

    options.AddTokenBucketLimiter("CartLimit", limiterOptions =>
    {
        limiterOptions.TokenLimit = 15;
        limiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        limiterOptions.TokensPerPeriod = 2;
        limiterOptions.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("FormLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

});

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Eticaret.Session";
    options.Cookie.HttpOnly = true; // JS ile okunmasını engeller (XSS koruması)
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict; // CSRF koruması
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.IOTimeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddScoped(typeof(IService<>), typeof(Service<>));
builder.Services.AddScoped<IOrderIyService, OrderIyService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton<IMaintenanceService, MaintenanceService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(p =>
{
    p.LoginPath = "/hesabim/giris-yap";
    p.AccessDeniedPath = "/404";

    p.Cookie.Name = "Account";
    p.Cookie.HttpOnly = true; // JS ile çerez hırsızlığını engeller
    p.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    p.Cookie.SameSite = SameSiteMode.Lax;
    p.Cookie.IsEssential = true;

    p.ExpireTimeSpan = TimeSpan.FromDays(1);
    p.SlidingExpiration = true; // Kullanıcı aktifse süreyi otomatik uzatır
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

// Authorization (Yetki) Ayarları
builder.Services.AddAuthorization(p =>
{
    p.AddPolicy("AdminPolicy", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    p.AddPolicy("UserPolicy", policy => policy.RequireClaim(ClaimTypes.Role, "Admin", "User"));
});
builder.Services.AddMemoryCache();

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

app.UseMiddleware<Eticaret.WebUI.Middlewares.MaintenanceMiddleware>(); // Bakım middlewarei 

app.UseRouting();

app.UseRateLimiter();
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

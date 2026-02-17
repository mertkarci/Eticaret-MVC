using System.Security.Claims;
using Eticaret.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<DatabaseContext>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie
(p =>
{
    p.LoginPath = "/Accounts/SignIn";
    p.AccessDeniedPath = "/AccessDenied";
    p.Cookie.Name = "Account";
    p.Cookie.MaxAge = TimeSpan.FromDays(1);
    p.Cookie.IsEssential = true;
});

builder.Services.AddAuthorization(p =>
{
    p.AddPolicy("AdminPolicy", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    p.AddPolicy("UserPolicy", policy => policy.RequireClaim(ClaimTypes.Role, "Admin","User","Customer"));
});
var app = builder.Build();

var supportedCultures = new[] { new System.Globalization.CultureInfo("tr-TR") };

// Para birimi formatını özelleştiriyoruz
supportedCultures[0].NumberFormat.CurrencySymbol = "₺";
supportedCultures[0].NumberFormat.CurrencyPositivePattern = 1;
// 0: ₺150
// 1: 150₺
// 2: ₺ 150
// 3: 150 ₺

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // önce oturum
app.UseAuthorization(); // sonra yetkilendirme

app.MapStaticAssets();
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Main}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

var path = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine("DB PATH = " + path);
app.Run();

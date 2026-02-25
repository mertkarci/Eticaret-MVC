using System.Reflection;
using Eticaret.Core;
using Eticaret.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.Data;

public class DatabaseContext : DbContext
{
    // --- 1. DÜZELTME: Constructor Eklendi ---
    // Program.cs'den gelen "options" (veritabanı yolu vb.) buraya gelir
    // ve base(options) diyerek EF Core'un ana yapısına gönderilir.
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Slider> Sliders { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ThemeSetting> ThemeSettings { get; set; }



    // --- 2. DÜZELTME: OnConfiguring SİLİNDİ ---
    // Buradaki OnConfiguring metodunu kaldırdım.
    // Çünkü veritabanı yolunu artık Program.cs içinde "dbPath" ile veriyoruz.
    // Burada kalırsa çakışma yaratır.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Varsayılan Tema Verileri (Seed Data)
        modelBuilder.Entity<ThemeSetting>().HasData(
            new ThemeSetting
            {
                Id = 1,
                Name = "Varsayılan Tema",
                MainColor = "#0d6efd",
                SecondaryColor = "#6c757d",
                BackgroundColor = "#f8f9fa",
                TextColor = "#212529",
                NavbarBgColor = "#ffffff",
                FooterBgColor = "#343a40",
                IsActive = true
            },
            new ThemeSetting
            {
                Id = 2,
                Name = "Yılbaşı Teması",
                MainColor = "#C1121F",
                SecondaryColor = "#2D6A4F",
                BackgroundColor = "#F8F9FA",
                TextColor = "#1B1B1B",
                NavbarBgColor = "#2D6A4F",
                FooterBgColor = "#1B4332",
                IsActive = false
            },
            new ThemeSetting
            {
                Id = 3,
                Name = "Dark Modern",

                MainColor = "#6366F1",        // Indigo vurgu
                SecondaryColor = "#22C55E",   // Yeşil aksan

                BackgroundColor = "#0F172A",  // Koyu lacivert
                TextColor = "#E5E7EB",        // Açık metin

                NavbarBgColor = "#020617",
                FooterBgColor = "#020617",

                IsActive = true
            },
            new ThemeSetting
            {
                Id = 4,
                Name = "Soft Nature",

                MainColor = "#4CAF50",        // Doğa yeşili
                SecondaryColor = "#A3D9A5",   // Açık yeşil

                BackgroundColor = "#F1F8F4",  // Ferah açık ton
                TextColor = "#263238",

                NavbarBgColor = "#FFFFFF",
                FooterBgColor = "#E8F5E9",

                IsActive = true
            }

        );
    }

}

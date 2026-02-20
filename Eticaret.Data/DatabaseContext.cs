using System.Reflection;
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



    // --- 2. DÜZELTME: OnConfiguring SİLİNDİ ---
    // Buradaki OnConfiguring metodunu kaldırdım.
    // Çünkü veritabanı yolunu artık Program.cs içinde "dbPath" ile veriyoruz.
    // Burada kalırsa çakışma yaratır.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    // Not: "AddSync" metodunu sildim çünkü gereksiz. 
    // EF Core'un kendi "AddAsync" veya "Add" metotları zaten var.
}
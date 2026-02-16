using System.Reflection;
using Eticaret.Core;
using Eticaret.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eticaret.Data;

public class DatabaseContext : DbContext
{
    public DbSet<AppUser> AppUsers {get; set;}
    public DbSet<Brand> Brands {get; set;}
    public DbSet<Category> Categories {get; set;}
    public DbSet<Contact> Contacts {get; set;}
    public DbSet<News> News {get; set;}
    public DbSet<Product> Products {get; set;}
    public DbSet<Slider> Sliders {get; set;}

    public async Task AddSync(AppUser appUser)
    {
        throw new NotImplementedException();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Eticaret.db");
        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

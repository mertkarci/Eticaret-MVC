using Eticaret.Core.Entities;
using Microsoft.EntityFrameworkCore; 
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Eticaret.Data;

internal class AppUserConfigurations : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasColumnType("varchar(50)").HasMaxLength(50);
        builder.Property(x => x.Surname).IsRequired().HasColumnType("varchar(50)").HasMaxLength(50);
        builder.Property(x => x.Email).IsRequired().HasColumnType("varchar(50)").HasMaxLength(50);
        builder.Property(x => x.Phone).HasColumnType("varchar(15)").HasMaxLength(15);
        builder.Property(x => x.Password).IsRequired().HasColumnType("nvarchar(150)").HasMaxLength(150);
        builder.Property(x => x.Username).HasColumnType("varchar(50)").HasMaxLength(50);
        builder.HasData(
            new AppUser
            {
                Id = 1,
                Name = "admins",
                Surname = "bey",
                Email = "admin@eticaret.com",
                Phone = "05522099919",
                Username = "admin",
                Password = "admin",
                isAdmin = true,
                isActive = true,
                UserGuid = Guid.Parse("d8663e5e-7494-4f81-8739-6e031c637579"),
                CreateDate = DateTime.Parse("2024-01-01")
            }
        );
    }
}

using Eticaret.Core;
using Eticaret.Core.Entities;
using Microsoft.EntityFrameworkCore; 
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Eticaret.Data;

internal class BrandConfigurations : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(50);
        builder.Property(x => x.Logo).HasMaxLength(50);
        builder.HasData(
            new Brand
            {
                Id = 1,
                Name = "New Brand",
                Description = "Example Description of Brand",
                Logo = "Example Logo of Brand",
                isActive = true,
                CreateDate = DateTime.Parse("2024-01-01")
            }
        );
    }
}

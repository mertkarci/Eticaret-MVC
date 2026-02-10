using Eticaret.Core;
using Eticaret.Core.Entities;
using Microsoft.EntityFrameworkCore; 
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Eticaret.Data.Configurations;

internal class CategoryConfigurations : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Image).HasMaxLength(50);
        builder.HasData(
            new Category
            {
                Id = 1,
                Name = "Elektronik",
                isTopMenu = true,
                isActive = true,
                ParentId = 0,
                OrderNo = 1,
                CreateDate = DateTime.Parse("2024-01-01")
            }
        );
    }
}

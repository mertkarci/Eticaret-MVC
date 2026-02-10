using Eticaret.Core;
using Eticaret.Core.Entities;
using Microsoft.EntityFrameworkCore; 
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Eticaret.Data;

internal class SliderConfigurations : IEntityTypeConfiguration<Slider>
{
    public void Configure(EntityTypeBuilder<Slider> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(50);
        builder.Property(x => x.Image).HasMaxLength(50);
        builder.Property(x => x.Link).HasMaxLength(100);
    }
}

using Eticaret.Core;
using Eticaret.Core.Entities;
using Microsoft.EntityFrameworkCore; 
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Eticaret.Data;

internal class ContactConfigurations : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Surname).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(50);
        builder.Property(x => x.Phone).HasColumnType("varchar(15)").HasMaxLength(15);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(500);
    }
}

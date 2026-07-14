using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class Icd11CodeConfiguration : IEntityTypeConfiguration<Icd11Code>
{
    public void Configure(EntityTypeBuilder<Icd11Code> builder)
    {
        builder.ToTable("icd11_codes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Description).IsRequired().HasMaxLength(500);

        builder.HasIndex(c => c.Code).IsUnique();
    }
}

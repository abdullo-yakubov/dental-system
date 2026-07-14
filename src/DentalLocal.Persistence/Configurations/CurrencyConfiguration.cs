using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Symbol)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.ExchangeRateToBase)
            .HasColumnType("numeric(12,4)")
            .IsRequired();

        builder.Property(c => c.IsBase)
            .IsRequired()
            .HasDefaultValue(false);
    }
}

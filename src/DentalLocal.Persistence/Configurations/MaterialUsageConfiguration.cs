using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class MaterialUsageConfiguration : IEntityTypeConfiguration<MaterialUsage>
{
    public void Configure(EntityTypeBuilder<MaterialUsage> builder)
    {
        builder.ToTable("material_usages");

        builder.HasKey(mu => mu.Id);

        builder.Property(mu => mu.QuantityUsed)
            .HasColumnType("numeric(10,2)")
            .IsRequired();

        builder.Property(mu => mu.PricePerUnitSnapshot)
            .HasColumnType("numeric(12,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.HasOne(mu => mu.Visit)
            .WithMany()
            .HasForeignKey(mu => mu.VisitId)
            .OnDelete(DeleteBehavior.Cascade); // cascade deletion of visit usages is acceptable as medical visits are immutable records deleted only under systemic resets

        builder.HasOne(mu => mu.Material)
            .WithMany()
            .HasForeignKey(mu => mu.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mu => mu.VisitId);
        builder.HasIndex(mu => mu.MaterialId);
    }
}

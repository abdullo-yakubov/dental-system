using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("materials");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(255).IsRequired();
        builder.Property(m => m.Unit).HasMaxLength(50).IsRequired();
        
        builder.Property(m => m.QuantityInStock)
            .HasColumnType("numeric(10,2)")
            .HasDefaultValue(0);
            
        builder.Property(m => m.MinStockThreshold)
            .HasColumnType("numeric(10,2)")
            .HasDefaultValue(0);
            
        builder.Property(m => m.PricePerUnit)
            .HasColumnType("numeric(12,2)")
            .HasDefaultValue(0);
            
        builder.HasIndex(m => m.Name);
    }
}

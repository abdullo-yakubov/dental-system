using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class DiscountHistoryConfiguration : IEntityTypeConfiguration<DiscountHistory>
{
    public void Configure(EntityTypeBuilder<DiscountHistory> builder)
    {
        builder.ToTable("discount_history");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DiscountType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Amount)
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(d => d.Percent)
            .HasColumnType("numeric(5,2)")
            .IsRequired();

        builder.Property(d => d.Reason)
            .HasMaxLength(250);

        builder.Property(d => d.AppliedAt)
            .IsRequired();

        builder.HasIndex(d => d.PatientId);
        builder.HasIndex(d => d.VisitId);
    }
}

using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class ProcedureConfiguration : IEntityTypeConfiguration<Procedure>
{
    public void Configure(EntityTypeBuilder<Procedure> builder)
    {
        builder.ToTable("procedures", t => t.HasCheckConstraint("ck_procedures_price_non_negative", "price >= 0"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description).HasColumnType("text");

        builder.Property(p => p.Price)
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(p => p.DurationMinutes)
            .HasDefaultValue(30)
            .IsRequired();

        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.RequiresTooth).HasDefaultValue(false);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Procedures)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.IsActive);
    }
}

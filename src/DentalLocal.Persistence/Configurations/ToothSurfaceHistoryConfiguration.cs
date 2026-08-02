using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class ToothSurfaceHistoryConfiguration : IEntityTypeConfiguration<ToothSurfaceHistory>
{
    public void Configure(EntityTypeBuilder<ToothSurfaceHistory> builder)
    {
        builder.ToTable("tooth_surface_history");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.SurfaceCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.Diagnosis).HasMaxLength(255);
        builder.Property(t => t.Icd10Code).HasMaxLength(20);
        builder.Property(t => t.Icd11Code).HasMaxLength(20);
        builder.Property(t => t.ChangedAt).IsRequired();
        builder.Property(t => t.DoctorComment).HasColumnType("text");

        builder.HasOne(t => t.Tooth)
            .WithMany()
            .HasForeignKey(t => t.ToothId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(t => t.VisitId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(t => new { t.ToothId, t.SurfaceCode });
        builder.HasIndex(t => t.ChangedAt);

        builder.UseXminAsConcurrencyToken(t => t.Version);
    }
}

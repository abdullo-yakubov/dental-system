using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class ToothStateHistoryConfiguration : IEntityTypeConfiguration<ToothStateHistory>
{
    public void Configure(EntityTypeBuilder<ToothStateHistory> builder)
    {
        builder.ToTable("tooth_state_history");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ToothNumber).IsRequired();
        builder.Property(t => t.OldState).IsRequired();
        builder.Property(t => t.NewState).IsRequired();
        builder.Property(t => t.ChangedAt).IsRequired();
        builder.Property(t => t.Comment).HasColumnType("text");

        builder.HasOne(t => t.Patient)
            .WithMany()
            .HasForeignKey(t => t.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(t => t.VisitId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(t => new { t.PatientId, t.ToothNumber });
    }
}

using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("prescriptions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.MedicationName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Dosage)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Instructions)
            .HasColumnType("text");

        builder.HasOne(p => p.Visit)
            .WithMany()
            .HasForeignKey(p => p.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.VisitId);
    }
}

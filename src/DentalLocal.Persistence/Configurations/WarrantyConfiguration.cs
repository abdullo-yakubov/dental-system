using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class WarrantyConfiguration : IEntityTypeConfiguration<Warranty>
{
    public void Configure(EntityTypeBuilder<Warranty> builder)
    {
        builder.ToTable("warranties");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.ToothNumber);

        builder.Property(w => w.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(w => w.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(w => w.Notes)
            .HasColumnType("text");

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(w => w.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(w => w.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Procedure>()
            .WithMany()
            .HasForeignKey(w => w.ProcedureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => w.PatientId);
        builder.HasIndex(w => w.VisitId);
    }
}

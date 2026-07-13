using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.StartsAt).IsRequired();
        builder.Property(a => a.DurationMinutes).IsRequired();
        builder.Property(a => a.Note).HasColumnType("text");
        builder.Property(a => a.Status).IsRequired();

        builder.HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Procedure)
            .WithMany()
            .HasForeignKey(a => a.ProcedureId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(a => a.VisitId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(a => a.StartsAt);
        builder.HasIndex(a => a.PatientId);
    }
}

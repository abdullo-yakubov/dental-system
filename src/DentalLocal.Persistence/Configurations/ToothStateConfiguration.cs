using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class ToothStateConfiguration : IEntityTypeConfiguration<ToothState>
{
    public void Configure(EntityTypeBuilder<ToothState> builder)
    {
        builder.ToTable("tooth_states");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ToothNumber).IsRequired();
        builder.Property(t => t.State).IsRequired();

        builder.HasOne(t => t.Patient)
            .WithMany()
            .HasForeignKey(t => t.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.PatientId, t.ToothNumber }).IsUnique();
    }
}

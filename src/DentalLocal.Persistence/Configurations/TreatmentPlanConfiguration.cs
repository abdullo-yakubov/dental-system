using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class TreatmentPlanConfiguration : IEntityTypeConfiguration<TreatmentPlan>
{
    public void Configure(EntityTypeBuilder<TreatmentPlan> builder)
    {
        builder.ToTable("treatment_plans");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Notes).HasColumnType("text");
        builder.Property(t => t.Status).IsRequired();

        builder.HasOne(t => t.Patient)
            .WithMany()
            .HasForeignKey(t => t.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Items)
            .WithOne(i => i.TreatmentPlan)
            .HasForeignKey(i => i.TreatmentPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.PatientId);
    }
}

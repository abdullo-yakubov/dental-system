using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        builder.ToTable("debts");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.OriginalAmount).HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(d => d.RemainingAmount).HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(d => d.AroseAt).IsRequired();
        builder.Property(d => d.Status).IsRequired();

        builder.HasOne(d => d.Visit)
            .WithOne()
            .HasForeignKey<Debt>(d => d.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.VisitId).IsUnique();

        builder.HasOne(d => d.Patient)
            .WithMany()
            .HasForeignKey(d => d.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.PatientId);
        builder.HasIndex(d => d.Status);
    }
}

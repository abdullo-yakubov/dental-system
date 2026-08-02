using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        // Инвариант знака и границ (см. промпт 20 аудита) — на уровне домена уже защищено Debt.ApplyPayment/
        // ReversePayment, но CHECK на БД не даёт даже прямому SQL/будущей миграции завести некорректную строку.
        builder.ToTable("debts", t => t.HasCheckConstraint(
            "ck_debts_remaining_amount_bounds", "remaining_amount >= 0 AND remaining_amount <= original_amount"));

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

using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace DentalLocal.Persistence.Configurations;

public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("visits");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VisitDate).HasColumnType("date").IsRequired();
        builder.Property(v => v.VisitTime).HasColumnType("time").IsRequired();
        builder.Property(v => v.DoctorName).HasMaxLength(150).IsRequired();

        builder.Property(v => v.Notes).HasColumnType("text");
        builder.Property(v => v.Diagnosis).HasColumnType("text");
        builder.Property(v => v.Prescriptions).HasColumnType("text");
        builder.Property(v => v.MaterialsUsed).HasColumnType("text");

        builder.Property(v => v.TotalCost).HasColumnType("numeric(12,2)");
        builder.Property(v => v.TotalPaid).HasColumnType("numeric(12,2)");
        builder.Property(v => v.Status).IsRequired();

        builder.HasOne(v => v.Patient)
            .WithMany()
            .HasForeignKey(v => v.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Items)
            .WithOne(i => i.Visit)
            .HasForeignKey(i => i.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.PatientId);
        builder.HasIndex(v => v.VisitDate);

        // Полнотекстовый поиск по заметкам врача и диагнозу (раздел 17.6 ТЗ).
        builder.Property<NpgsqlTsVector>("NotesSearchVector")
            .HasComputedColumnSql(
                "to_tsvector('russian', coalesce(notes, '') || ' ' || coalesce(diagnosis, ''))",
                stored: true);

        builder.HasIndex("NotesSearchVector").HasMethod("GIN").HasDatabaseName("ix_visits_notes_search_vector");
    }
}

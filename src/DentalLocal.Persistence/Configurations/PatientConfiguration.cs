using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace DentalLocal.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.CardNumber)
            .UseIdentityAlwaysColumn()
            .IsRequired();

        builder.HasIndex(p => p.CardNumber).IsUnique();

        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.MiddleName).HasMaxLength(100);

        builder.Property(p => p.Gender).IsRequired();
        builder.Property(p => p.BirthDate).HasColumnType("date").IsRequired();

        builder.Property(p => p.Phone).HasMaxLength(20).IsRequired();
        builder.Property(p => p.PhoneExtra).HasMaxLength(20);
        builder.Property(p => p.Address).HasMaxLength(300);

        builder.Property(p => p.Allergies).HasColumnType("text");
        builder.Property(p => p.ChronicDiseases).HasColumnType("text");
        builder.Property(p => p.Comments).HasColumnType("text");
        builder.Property(p => p.DoctorNotes).HasColumnType("text");

        builder.Property(p => p.RegisteredAt).IsRequired();
        builder.Property(p => p.IsArchived).HasDefaultValue(false);

        builder.HasOne<PatientFile>()
            .WithMany()
            .HasForeignKey(p => p.PhotoFileId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(p => p.Phone);
        builder.HasIndex(p => new { p.LastName, p.FirstName });
        builder.HasIndex(p => p.IsArchived)
            .HasFilter("NOT is_archived")
            .HasDatabaseName("ix_patients_not_archived");

        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasComputedColumnSql(
                "to_tsvector('russian', " +
                "coalesce(last_name, '') || ' ' || coalesce(first_name, '') || ' ' || coalesce(middle_name, '') || " +
                "' ' || coalesce(phone, '') || ' ' || coalesce(address, ''))",
                stored: true);

        builder.HasIndex("SearchVector").HasMethod("GIN").HasDatabaseName("ix_patients_search_vector");

        builder.HasIndex(p => p.LastName).HasMethod("gin").HasOperators(["gin_trgm_ops"]).HasDatabaseName("ix_patients_last_name_trgm");
    }
}

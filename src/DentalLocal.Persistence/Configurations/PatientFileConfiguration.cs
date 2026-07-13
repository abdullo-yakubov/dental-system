using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class PatientFileConfiguration : IEntityTypeConfiguration<PatientFile>
{
    public void Configure(EntityTypeBuilder<PatientFile> builder)
    {
        builder.ToTable("patient_files");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileType).IsRequired();
        builder.Property(f => f.FileName).HasMaxLength(300).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(f => f.SizeBytes).IsRequired();
        builder.Property(f => f.StoredPath).HasMaxLength(500).IsRequired();
        builder.Property(f => f.UploadedAt).IsRequired();
        builder.Property(f => f.Description).HasColumnType("text");

        builder.HasOne(f => f.Patient)
            .WithMany()
            .HasForeignKey(f => f.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => f.PatientId);
    }
}

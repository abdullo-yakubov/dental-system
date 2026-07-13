using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class BackupRecordConfiguration : IEntityTypeConfiguration<BackupRecord>
{
    public void Configure(EntityTypeBuilder<BackupRecord> builder)
    {
        builder.ToTable("backup_records");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.FileName).HasMaxLength(300).IsRequired();
        builder.Property(b => b.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(b => b.SizeBytes).IsRequired();
        builder.Property(b => b.Trigger).IsRequired();
        builder.Property(b => b.IsValid).IsRequired();
        builder.Property(b => b.Note).HasColumnType("text");

        builder.HasIndex(b => b.CreatedAt);
    }
}

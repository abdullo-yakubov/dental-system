using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class WaitingListConfiguration : IEntityTypeConfiguration<WaitingList>
{
    public void Configure(EntityTypeBuilder<WaitingList> builder)
    {
        builder.ToTable("waiting_list");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.DesiredDateFrom)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(w => w.DesiredDateTo)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(w => w.Status)
            .HasMaxLength(50)
            .HasDefaultValue("Waiting")
            .IsRequired();

        builder.Property(w => w.Notes)
            .HasColumnType("text");

        builder.HasOne(w => w.Patient)
            .WithMany()
            .HasForeignKey(w => w.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Procedure)
            .WithMany()
            .HasForeignKey(w => w.ProcedureId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(w => w.PatientId);
        builder.HasIndex(w => w.Status);
    }
}

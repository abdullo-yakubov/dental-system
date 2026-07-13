using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class TreatmentItemConfiguration : IEntityTypeConfiguration<TreatmentItem>
{
    public void Configure(EntityTypeBuilder<TreatmentItem> builder)
    {
        builder.ToTable("treatment_items", t => t.HasCheckConstraint(
            "ck_treatment_items_tooth_number",
            "tooth_number IS NULL OR tooth_number IN (11,12,13,14,15,16,17,18,21,22,23,24,25,26,27,28," +
            "31,32,33,34,35,36,37,38,41,42,43,44,45,46,47,48)"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ToothNumber);
        builder.Property(i => i.Price).HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(i => i.Quantity).HasDefaultValue(1);
        builder.Property(i => i.Comment).HasColumnType("text");

        builder.HasOne(i => i.Procedure)
            .WithMany()
            .HasForeignKey(i => i.ProcedureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.VisitId);
        builder.HasIndex(i => i.ProcedureId);
    }
}

using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", t => t.HasCheckConstraint("ck_payments_amount_positive", "amount > 0"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(p => p.PaidAt).IsRequired();
        builder.Property(p => p.Method).IsRequired();
        builder.Property(p => p.Kind).IsRequired();
        builder.Property(p => p.Note).HasColumnType("text");

        builder.HasOne(p => p.Visit)
            .WithMany()
            .HasForeignKey(p => p.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(p => p.ReversalOfPaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(p => p.VisitId);
        builder.HasIndex(p => p.PatientId);
    }
}

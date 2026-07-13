using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class ProcedureCategoryConfiguration : IEntityTypeConfiguration<ProcedureCategory>
{
    public void Configure(EntityTypeBuilder<ProcedureCategory> builder)
    {
        builder.ToTable("procedure_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(c => c.Color)
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(c => c.SortOrder).IsRequired();

        builder.HasMany(c => c.Procedures)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("doctor_schedules");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.DoctorName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.DayOfWeek)
            .IsRequired();

        builder.Property(s => s.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(s => s.EndTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(s => new { s.DoctorName, s.DayOfWeek });
    }
}

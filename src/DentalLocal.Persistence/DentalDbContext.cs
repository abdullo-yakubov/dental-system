using System.Reflection;
using DentalLocal.Domain.Entities;
using DentalLocal.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence;

public class DentalDbContext(DbContextOptions<DentalDbContext> options) : DbContext(options)
{
    public const string Schema = "dental";

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<ProcedureCategory> ProcedureCategories => Set<ProcedureCategory>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<TreatmentItem> TreatmentItems => Set<TreatmentItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<ToothState> ToothStates => Set<ToothState>();
    public DbSet<ToothStateHistory> ToothStateHistory => Set<ToothStateHistory>();
    public DbSet<ToothSurfaceHistory> ToothSurfaceHistory => Set<ToothSurfaceHistory>();
    public DbSet<Icd10Code> Icd10Codes => Set<Icd10Code>();
    public DbSet<Icd11Code> Icd11Codes => Set<Icd11Code>();
    public DbSet<PatientFile> PatientFiles => Set<PatientFile>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TreatmentPlanItem> TreatmentPlanItems => Set<TreatmentPlanItem>();

    public DbSet<DiscountHistory> DiscountHistories => Set<DiscountHistory>();
    public DbSet<WaitingList> WaitingLists => Set<WaitingList>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<ClinicSchedule> ClinicSchedules => Set<ClinicSchedule>();
    public DbSet<PatientDebtView> PatientDebts => Set<PatientDebtView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<PatientDebtView>(eb =>
        {
            eb.HasNoKey();
            eb.ToView("patient_debts");
        });

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.IImmutableRecord>())
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException(
                    $"Физическое удаление сущности «{entry.Entity.GetType().Name}» запрещено доменными правилами (см. п. O-6, 9.4, 9.6 ТЗ).");
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

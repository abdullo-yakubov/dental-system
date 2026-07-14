namespace DentalLocal.Domain.Repositories;

public interface IUnitOfWork
{
    IPatientRepository Patients { get; }
    IProcedureCategoryRepository ProcedureCategories { get; }
    IProcedureRepository Procedures { get; }
    IVisitRepository Visits { get; }
    IPaymentRepository Payments { get; }
    IDebtRepository Debts { get; }
    IToothStateRepository ToothStates { get; }
    IToothStateHistoryRepository ToothStateHistory { get; }
    IPatientFileRepository PatientFiles { get; }
    IAppointmentRepository Appointments { get; }
    IBackupRecordRepository BackupRecords { get; }
    IAuditLogRepository AuditLogs { get; }
    IAppSettingRepository AppSettings { get; }
    IMaterialRepository Materials { get; }
    IMaterialUsageRepository MaterialUsages { get; }
    IReminderRepository Reminders { get; }
    ITreatmentPlanRepository TreatmentPlans { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

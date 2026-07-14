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
    IToothSurfaceHistoryRepository ToothSurfaceHistory { get; }
    IIcd10CodeRepository Icd10Codes { get; }
    IIcd11CodeRepository Icd11Codes { get; }
    IPatientFileRepository PatientFiles { get; }
    IAppointmentRepository Appointments { get; }
    IBackupRecordRepository BackupRecords { get; }
    IAuditLogRepository AuditLogs { get; }
    IAppSettingRepository AppSettings { get; }
    IMaterialRepository Materials { get; }
    IMaterialUsageRepository MaterialUsages { get; }
    IReminderRepository Reminders { get; }
    ITreatmentPlanRepository TreatmentPlans { get; }
    ICurrencyRepository Currencies { get; }
    IDiscountHistoryRepository DiscountHistory { get; }
    IWaitingListRepository WaitingList { get; }
    IPrescriptionRepository Prescriptions { get; }
    IWarrantyRepository Warranties { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

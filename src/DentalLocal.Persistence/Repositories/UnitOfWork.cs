using DentalLocal.Domain.Repositories;

namespace DentalLocal.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DentalDbContext _context;

    public UnitOfWork(
        DentalDbContext context,
        IPatientRepository patients,
        IProcedureCategoryRepository procedureCategories,
        IProcedureRepository procedures,
        IVisitRepository visits,
        IPaymentRepository payments,
        IDebtRepository debts,
        IToothStateRepository toothStates,
        IToothStateHistoryRepository toothStateHistory,
        IPatientFileRepository patientFiles,
        IAppointmentRepository appointments,
        IBackupRecordRepository backupRecords,
        IAuditLogRepository auditLogs,
        IAppSettingRepository appSettings,
        IMaterialRepository materials,
        IMaterialUsageRepository materialUsages,
        IReminderRepository reminders,
        ITreatmentPlanRepository treatmentPlans)
    {
        _context = context;
        Patients = patients;
        ProcedureCategories = procedureCategories;
        Procedures = procedures;
        Visits = visits;
        Payments = payments;
        Debts = debts;
        ToothStates = toothStates;
        ToothStateHistory = toothStateHistory;
        PatientFiles = patientFiles;
        Appointments = appointments;
        BackupRecords = backupRecords;
        AuditLogs = auditLogs;
        AppSettings = appSettings;
        Materials = materials;
        MaterialUsages = materialUsages;
        Reminders = reminders;
        TreatmentPlans = treatmentPlans;
    }

    public IPatientRepository Patients { get; }
    public IProcedureCategoryRepository ProcedureCategories { get; }
    public IProcedureRepository Procedures { get; }
    public IVisitRepository Visits { get; }
    public IPaymentRepository Payments { get; }
    public IDebtRepository Debts { get; }
    public IToothStateRepository ToothStates { get; }
    public IToothStateHistoryRepository ToothStateHistory { get; }
    public IPatientFileRepository PatientFiles { get; }
    public IAppointmentRepository Appointments { get; }
    public IBackupRecordRepository BackupRecords { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IAppSettingRepository AppSettings { get; }
    public IMaterialRepository Materials { get; }
    public IMaterialUsageRepository MaterialUsages { get; }
    public IReminderRepository Reminders { get; }
    public ITreatmentPlanRepository TreatmentPlans { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransactionScope(transaction);
    }
}

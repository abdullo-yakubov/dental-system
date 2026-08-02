using DentalLocal.Domain.Exceptions;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

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
        IToothSurfaceHistoryRepository toothSurfaceHistory,
        IIcd10CodeRepository icd10Codes,
        IIcd11CodeRepository icd11Codes,
        IPatientFileRepository patientFiles,
        IAppointmentRepository appointments,
        IBackupRecordRepository backupRecords,
        IAuditLogRepository auditLogs,
        IAppSettingRepository appSettings,
        IMaterialRepository materials,
        IMaterialUsageRepository materialUsages,
        IReminderRepository reminders,
        ITreatmentPlanRepository treatmentPlans,
        IDiscountHistoryRepository discountHistory,
        IWaitingListRepository waitingList,
        IPrescriptionRepository prescriptions,
        IWarrantyRepository warranties,
        IDoctorScheduleRepository doctorSchedules,
        IClinicScheduleRepository clinicSchedules)
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
        ToothSurfaceHistory = toothSurfaceHistory;
        Icd10Codes = icd10Codes;
        Icd11Codes = icd11Codes;
        PatientFiles = patientFiles;
        Appointments = appointments;
        BackupRecords = backupRecords;
        AuditLogs = auditLogs;
        AppSettings = appSettings;
        Materials = materials;
        MaterialUsages = materialUsages;
        Reminders = reminders;
        TreatmentPlans = treatmentPlans;
        DiscountHistory = discountHistory;
        WaitingList = waitingList;
        Prescriptions = prescriptions;
        Warranties = warranties;
        DoctorSchedules = doctorSchedules;
        ClinicSchedules = clinicSchedules;
    }

    public IPatientRepository Patients { get; }
    public IProcedureCategoryRepository ProcedureCategories { get; }
    public IProcedureRepository Procedures { get; }
    public IVisitRepository Visits { get; }
    public IPaymentRepository Payments { get; }
    public IDebtRepository Debts { get; }
    public IToothStateRepository ToothStates { get; }
    public IToothStateHistoryRepository ToothStateHistory { get; }
    public IToothSurfaceHistoryRepository ToothSurfaceHistory { get; }
    public IIcd10CodeRepository Icd10Codes { get; }
    public IIcd11CodeRepository Icd11Codes { get; }
    public IPatientFileRepository PatientFiles { get; }
    public IAppointmentRepository Appointments { get; }
    public IBackupRecordRepository BackupRecords { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IAppSettingRepository AppSettings { get; }
    public IMaterialRepository Materials { get; }
    public IMaterialUsageRepository MaterialUsages { get; }
    public IReminderRepository Reminders { get; }
    public ITreatmentPlanRepository TreatmentPlans { get; }
    public IDiscountHistoryRepository DiscountHistory { get; }
    public IWaitingListRepository WaitingList { get; }
    public IPrescriptionRepository Prescriptions { get; }
    public IWarrantyRepository Warranties { get; }
    public IDoctorScheduleRepository DoctorSchedules { get; }
    public IClinicScheduleRepository ClinicSchedules { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("Данные были изменены другим процессом. Обновите страницу и повторите попытку.", ex);
        }
    }

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransactionScope(transaction);
    }
}

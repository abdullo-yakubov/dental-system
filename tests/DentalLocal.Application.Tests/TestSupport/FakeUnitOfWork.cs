using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;

namespace DentalLocal.Application.Tests.TestSupport;

// Минимальный in-memory дублёр IUnitOfWork для юнит-тестов Application-обработчиков —
// реализует только то, что реально используется в Payments-хендлерах; остальные
// репозитории намеренно бросают NotImplementedException, если их вызовут в тесте по ошибке.
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public readonly List<Visit> VisitsData = [];
    public readonly List<Patient> PatientsData = [];
    public readonly List<Payment> PaymentsData = [];
    public readonly List<Debt> DebtsData = [];
    public readonly List<AuditLog> AuditLogsData = [];
    public readonly List<Procedure> ProceduresData = [];
    public readonly List<ToothState> ToothStatesData = [];
    public readonly List<ToothStateHistory> ToothStateHistoryData = [];
    public readonly List<ToothSurfaceHistory> ToothSurfaceHistoryData = [];
    public readonly List<Material> MaterialsData = [];
    public readonly List<MaterialUsage> MaterialUsagesData = [];
    public readonly List<Reminder> RemindersData = [];
    public readonly List<TreatmentPlan> TreatmentPlansData = [];
    public readonly List<DiscountHistory> DiscountHistoryData = [];
    public readonly List<WaitingList> WaitingListData = [];
    public readonly List<Prescription> PrescriptionsData = [];
    public readonly List<Warranty> WarrantiesData = [];

    public IPatientRepository Patients => new FakePatientRepository(PatientsData);
    public IVisitRepository Visits => new FakeVisitRepository(VisitsData);
    public IPaymentRepository Payments => new FakePaymentRepository(PaymentsData);
    public IDebtRepository Debts => new FakeDebtRepository(DebtsData);
    public IAuditLogRepository AuditLogs => new FakeAuditLogRepository(AuditLogsData);

    public IProcedureCategoryRepository ProcedureCategories => throw new NotImplementedException();
    public IProcedureRepository Procedures => new FakeProcedureRepository(ProceduresData);
    public IToothStateRepository ToothStates => new FakeToothStateRepository(ToothStatesData);
    public IToothStateHistoryRepository ToothStateHistory => new FakeToothStateHistoryRepository(ToothStateHistoryData);
    public IToothSurfaceHistoryRepository ToothSurfaceHistory => new FakeToothSurfaceHistoryRepository(ToothSurfaceHistoryData);
    public IIcd10CodeRepository Icd10Codes => throw new NotImplementedException();
    public IIcd11CodeRepository Icd11Codes => throw new NotImplementedException();
    public IPatientFileRepository PatientFiles => throw new NotImplementedException();
    public IAppointmentRepository Appointments => throw new NotImplementedException();
    public IBackupRecordRepository BackupRecords => throw new NotImplementedException();
    public IAppSettingRepository AppSettings => throw new NotImplementedException();
    public IMaterialRepository Materials => new FakeMaterialRepository(MaterialsData);
    public IMaterialUsageRepository MaterialUsages => new FakeMaterialUsageRepository(MaterialUsagesData);
    public IReminderRepository Reminders => new FakeReminderRepository(RemindersData);
    public ITreatmentPlanRepository TreatmentPlans => new FakeTreatmentPlanRepository(TreatmentPlansData);
    public IDiscountHistoryRepository DiscountHistory => new FakeDiscountHistoryRepository(DiscountHistoryData);
    public IWaitingListRepository WaitingList => new FakeWaitingListRepository(WaitingListData);
    public IPrescriptionRepository Prescriptions => new FakePrescriptionRepository(PrescriptionsData);
    public IWarrantyRepository Warranties => new FakeWarrantyRepository(WarrantiesData);
    public IDoctorScheduleRepository DoctorSchedules => throw new NotImplementedException();
    public IClinicScheduleRepository ClinicSchedules => throw new NotImplementedException();

    public int BeginTransactionCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        BeginTransactionCallCount++;
        return Task.FromResult<ITransactionScope>(new NoOpTransactionScope());
    }

    private sealed class NoOpTransactionScope : ITransactionScope
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeVisitRepository(List<Visit> store) : IVisitRepository
    {
        public Task<Visit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(v => v.Id == id));

        public Task<Visit?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(v => v.Id == id));

        public Task<IReadOnlyList<Visit>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Visit>>(store.Where(v => v.PatientId == patientId).ToList());

        public Task<IReadOnlyList<Visit>> GetUpcomingAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Visit>>([]);

        public Task<(IReadOnlyList<Visit> Items, int TotalCount)> SearchByNotesAsync(
            string searchText, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Visit>, int)>(([], 0));

        public Task<IReadOnlyList<Visit>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Visit>>(store.Where(v => v.VisitDate >= from && v.VisitDate <= to).ToList());

        public Task AddAsync(Visit entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePatientRepository(List<Patient> store) : IPatientRepository
    {
        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(p => p.Id == id));

        public Task<Patient?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(p => p.Id == id));

        public Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchAsync(
            string? search, bool includeArchived, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Patient>, int)>(([], 0));

        public Task<bool> PhoneExistsAsync(string normalizedPhone, Guid? excludePatientId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<Patient>> FindPossibleDuplicatesAsync(
            string lastName, string firstName, DateOnly birthDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Patient>>([]);

        public Task<IReadOnlyList<Patient>> GetInactiveAsync(int monthsThreshold, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Patient>>([]);

        public Task DeleteWithRelatedDataAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            store.RemoveAll(p => p.Id == patientId);
            return Task.CompletedTask;
        }

        public Task AddAsync(Patient entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentRepository(List<Payment> store) : IPaymentRepository
    {
        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<Payment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Payment>>(store.Where(p => p.PatientId == patientId).ToList());

        public Task<IReadOnlyList<Payment>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Payment>>(store.Where(p => p.VisitId == visitId).ToList());

        public Task AddAsync(Payment entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDebtRepository(List<Debt> store) : IDebtRepository
    {
        public Task<Debt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(d => d.Id == id));

        public Task<Debt?> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(d => d.VisitId == visitId));

        public Task<IReadOnlyList<Debt>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Debt>>(store.Where(d => d.PatientId == patientId).ToList());

        public Task<(IReadOnlyList<Debt> Items, int TotalCount)> SearchAsync(
            DebtStatus? status, string? search, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Debt>, int)>(([], 0));

        public Task AddAsync(Debt entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditLogRepository(List<AuditLog> store) : IAuditLogRepository
    {
        public Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(a => a.Id == id));

        public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
            int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<AuditLog>, int)>(([], 0));

        public Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProcedureRepository(List<Procedure> store) : IProcedureRepository
    {
        public Task<Procedure?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<Procedure>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Procedure>>(store.Where(p => includeInactive || p.IsActive).ToList());

        public Task<bool> IsUsedInTreatmentAsync(Guid procedureId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(Procedure entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeToothStateRepository(List<ToothState> store) : IToothStateRepository
    {
        public Task<ToothState?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<ToothState>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ToothState>>(store.Where(t => t.PatientId == patientId).ToList());

        public Task<ToothState?> GetAsync(Guid patientId, short toothNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(t => t.PatientId == patientId && t.ToothNumber == toothNumber));

        public Task AddAsync(ToothState entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeToothStateHistoryRepository(List<ToothStateHistory> store) : IToothStateHistoryRepository
    {
        public Task<ToothStateHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(h => h.Id == id));

        public Task<IReadOnlyList<ToothStateHistory>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ToothStateHistory>>(store.Where(h => h.PatientId == patientId).ToList());

        public Task<IReadOnlyList<ToothStateHistory>> GetByToothAsync(Guid patientId, short toothNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ToothStateHistory>>(
                store.Where(h => h.PatientId == patientId && h.ToothNumber == toothNumber).ToList());

        public Task AddAsync(ToothStateHistory entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeToothSurfaceHistoryRepository(List<ToothSurfaceHistory> store) : IToothSurfaceHistoryRepository
    {
        public Task<ToothSurfaceHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(h => h.Id == id));

        public Task<IReadOnlyList<ToothSurfaceHistory>> GetByToothIdAsync(Guid toothId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ToothSurfaceHistory>>(store.Where(h => h.ToothId == toothId).ToList());

        public Task<IReadOnlyList<ToothSurfaceHistory>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ToothSurfaceHistory>>([]);

        public Task AddAsync(ToothSurfaceHistory entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMaterialRepository(List<Material> store) : IMaterialRepository
    {
        public Task<Material?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(m => m.Id == id));

        public Task<IReadOnlyList<Material>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Material>>(store.ToList());

        public Task<IReadOnlyList<Material>> GetLowStockAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Material>>(store.Where(m => m.QuantityInStock <= m.MinStockThreshold).ToList());

        public Task AddAsync(Material entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMaterialUsageRepository(List<MaterialUsage> store) : IMaterialUsageRepository
    {
        public Task<MaterialUsage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(u => u.Id == id));

        public Task<IReadOnlyList<MaterialUsage>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MaterialUsage>>(store.Where(u => u.VisitId == visitId).ToList());

        public Task<IReadOnlyList<MaterialUsage>> GetByVisitIdsAsync(IReadOnlyCollection<Guid> visitIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MaterialUsage>>(store.Where(u => visitIds.Contains(u.VisitId)).ToList());

        public Task AddAsync(MaterialUsage entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReminderRepository(List<Reminder> store) : IReminderRepository
    {
        public Task<Reminder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(r => r.Id == id));

        public Task AddAsync(Reminder entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTreatmentPlanRepository(List<TreatmentPlan> store) : ITreatmentPlanRepository
    {
        public Task<TreatmentPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(p => p.Id == id));

        public Task<TreatmentPlan?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<TreatmentPlan>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TreatmentPlan>>(store.Where(p => p.PatientId == patientId).ToList());

        public Task AddAsync(TreatmentPlan entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDiscountHistoryRepository(List<DiscountHistory> store) : IDiscountHistoryRepository
    {
        public Task<DiscountHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(d => d.Id == id));

        public Task<IReadOnlyList<DiscountHistory>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiscountHistory>>(store.Where(d => d.PatientId == patientId).ToList());

        public Task AddAsync(DiscountHistory entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWaitingListRepository(List<WaitingList> store) : IWaitingListRepository
    {
        public Task<WaitingList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(w => w.Id == id));

        public Task<IReadOnlyList<WaitingList>> GetAllAsync(string? status, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WaitingList>>(
                (string.IsNullOrWhiteSpace(status) ? store : store.Where(w => w.Status == status)).ToList());

        public Task<IReadOnlyList<WaitingList>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WaitingList>>(store.Where(w => w.PatientId == patientId).ToList());

        public Task AddAsync(WaitingList entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePrescriptionRepository(List<Prescription> store) : IPrescriptionRepository
    {
        public Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<Prescription>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Prescription>>(store.Where(p => p.VisitId == visitId).ToList());

        public Task DeleteAsync(Prescription prescription, CancellationToken cancellationToken = default)
        {
            store.Remove(prescription);
            return Task.CompletedTask;
        }

        public Task AddAsync(Prescription entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWarrantyRepository(List<Warranty> store) : IWarrantyRepository
    {
        public Task<Warranty?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.FirstOrDefault(w => w.Id == id));

        public Task<IReadOnlyList<Warranty>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Warranty>>(store.Where(w => w.PatientId == patientId).ToList());

        public Task<IReadOnlyList<Warranty>> GetByToothNumberAsync(Guid patientId, short toothNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Warranty>>(
                store.Where(w => w.PatientId == patientId && w.ToothNumber == toothNumber).ToList());

        public Task AddAsync(Warranty entity, CancellationToken cancellationToken = default)
        {
            store.Add(entity);
            return Task.CompletedTask;
        }
    }
}

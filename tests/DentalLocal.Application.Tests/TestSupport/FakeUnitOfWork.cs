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

    public IPatientRepository Patients => new FakePatientRepository(PatientsData);
    public IVisitRepository Visits => new FakeVisitRepository(VisitsData);
    public IPaymentRepository Payments => new FakePaymentRepository(PaymentsData);
    public IDebtRepository Debts => new FakeDebtRepository(DebtsData);
    public IAuditLogRepository AuditLogs => new FakeAuditLogRepository(AuditLogsData);

    public IProcedureCategoryRepository ProcedureCategories => throw new NotImplementedException();
    public IProcedureRepository Procedures => throw new NotImplementedException();
    public IToothStateRepository ToothStates => throw new NotImplementedException();
    public IToothStateHistoryRepository ToothStateHistory => throw new NotImplementedException();
    public IToothSurfaceHistoryRepository ToothSurfaceHistory => throw new NotImplementedException();
    public IIcd10CodeRepository Icd10Codes => throw new NotImplementedException();
    public IIcd11CodeRepository Icd11Codes => throw new NotImplementedException();
    public IPatientFileRepository PatientFiles => throw new NotImplementedException();
    public IAppointmentRepository Appointments => throw new NotImplementedException();
    public IBackupRecordRepository BackupRecords => throw new NotImplementedException();
    public IAppSettingRepository AppSettings => throw new NotImplementedException();
    public IMaterialRepository Materials => throw new NotImplementedException();
    public IMaterialUsageRepository MaterialUsages => throw new NotImplementedException();
    public IReminderRepository Reminders => throw new NotImplementedException();
    public ITreatmentPlanRepository TreatmentPlans => throw new NotImplementedException();
    public ICurrencyRepository Currencies => throw new NotImplementedException();
    public IDiscountHistoryRepository DiscountHistory => throw new NotImplementedException();
    public IWaitingListRepository WaitingList => throw new NotImplementedException();
    public IPrescriptionRepository Prescriptions => throw new NotImplementedException();
    public IWarrantyRepository Warranties => throw new NotImplementedException();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ITransactionScope>(new NoOpTransactionScope());

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
}

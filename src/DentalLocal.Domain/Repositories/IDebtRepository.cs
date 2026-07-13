using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Repositories;

public interface IDebtRepository : IRepository<Debt>
{
    Task<Debt?> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Debt>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Debt> Items, int TotalCount)> SearchAsync(
        DebtStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

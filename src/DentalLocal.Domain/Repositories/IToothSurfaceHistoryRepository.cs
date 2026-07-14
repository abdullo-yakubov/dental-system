using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IToothSurfaceHistoryRepository : IRepository<ToothSurfaceHistory>
{
    Task<IReadOnlyList<ToothSurfaceHistory>> GetByToothIdAsync(Guid toothId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ToothSurfaceHistory>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}

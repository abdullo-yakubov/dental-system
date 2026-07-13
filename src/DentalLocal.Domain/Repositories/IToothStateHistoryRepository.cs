using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IToothStateHistoryRepository : IRepository<ToothStateHistory>
{
    Task<IReadOnlyList<ToothStateHistory>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ToothStateHistory>> GetByToothAsync(Guid patientId, short toothNumber, CancellationToken cancellationToken = default);
}

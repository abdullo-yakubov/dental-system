using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IToothStateRepository : IRepository<ToothState>
{
    Task<IReadOnlyList<ToothState>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<ToothState?> GetAsync(Guid patientId, short toothNumber, CancellationToken cancellationToken = default);
}

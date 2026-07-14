using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IWarrantyRepository : IRepository<Warranty>
{
    Task<IReadOnlyList<Warranty>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Warranty>> GetByToothNumberAsync(Guid patientId, short toothNumber, CancellationToken cancellationToken = default);
}

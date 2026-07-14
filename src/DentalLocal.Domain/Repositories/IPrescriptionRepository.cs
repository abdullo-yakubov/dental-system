using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IPrescriptionRepository : IRepository<Prescription>
{
    Task<IReadOnlyList<Prescription>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Prescription prescription, CancellationToken cancellationToken = default);
}

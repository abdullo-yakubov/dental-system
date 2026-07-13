using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IVisitRepository : IRepository<Visit>
{
    Task<Visit?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Visit>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}

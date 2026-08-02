using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IMaterialUsageRepository : IRepository<MaterialUsage>
{
    Task<IReadOnlyList<MaterialUsage>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialUsage>> GetByVisitIdsAsync(IReadOnlyCollection<Guid> visitIds, CancellationToken cancellationToken = default);
}

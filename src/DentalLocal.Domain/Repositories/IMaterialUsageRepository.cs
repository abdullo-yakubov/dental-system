using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IMaterialUsageRepository : IRepository<MaterialUsage>
{
    Task<IReadOnlyList<MaterialUsage>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default);
}

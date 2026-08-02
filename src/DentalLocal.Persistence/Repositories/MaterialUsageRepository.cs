using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class MaterialUsageRepository(DentalDbContext context) : RepositoryBase<MaterialUsage>(context), IMaterialUsageRepository
{
    public async Task<IReadOnlyList<MaterialUsage>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(mu => mu.Material)
            .Where(mu => mu.VisitId == visitId)
            .OrderBy(mu => mu.UsedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MaterialUsage>> GetByVisitIdsAsync(IReadOnlyCollection<Guid> visitIds, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(mu => mu.Material)
            .Where(mu => visitIds.Contains(mu.VisitId))
            .ToListAsync(cancellationToken);
}

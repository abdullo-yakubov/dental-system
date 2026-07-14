using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class MaterialRepository(DentalDbContext context) : RepositoryBase<Material>(context), IMaterialRepository
{
    public async Task<IReadOnlyList<Material>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Material>> GetLowStockAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(m => m.QuantityInStock <= m.MinStockThreshold)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
}

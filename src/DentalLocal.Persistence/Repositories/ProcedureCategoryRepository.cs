using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class ProcedureCategoryRepository(DentalDbContext context)
    : RepositoryBase<ProcedureCategory>(context), IProcedureCategoryRepository
{
    public async Task<IReadOnlyList<ProcedureCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = Set.Where(c => c.Name == name);

        if (excludeId is not null)
            query = query.Where(c => c.Id != excludeId);

        return await query.AnyAsync(cancellationToken);
    }
}

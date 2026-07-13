using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class ProcedureRepository(DentalDbContext context) : RepositoryBase<Procedure>(context), IProcedureRepository
{
    public async Task<IReadOnlyList<Procedure>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        return await query.OrderBy(p => p.Category.SortOrder).ThenBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> IsUsedInTreatmentAsync(Guid procedureId, CancellationToken cancellationToken = default) =>
        Context.Set<TreatmentItem>().AnyAsync(i => i.ProcedureId == procedureId, cancellationToken);
}

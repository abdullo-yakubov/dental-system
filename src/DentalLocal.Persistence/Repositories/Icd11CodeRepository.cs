using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class Icd11CodeRepository(DentalDbContext context)
    : RepositoryBase<Icd11Code>(context), IIcd11CodeRepository
{
    public async Task<IReadOnlyList<Icd11Code>> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default)
    {
        var q = Set.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            q = q.Where(c => EF.Functions.ILike(c.Code, pattern) || EF.Functions.ILike(c.Description, pattern));
        }

        return await q.OrderBy(c => c.Code).Take(limit).ToListAsync(cancellationToken);
    }
}

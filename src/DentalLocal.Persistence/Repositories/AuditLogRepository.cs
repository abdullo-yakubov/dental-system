using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class AuditLogRepository(DentalDbContext context) : RepositoryBase<AuditLog>(context), IAuditLogRepository
{
    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().OrderByDescending(x => x.OccurredAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

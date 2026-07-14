using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IIcd11CodeRepository : IRepository<Icd11Code>
{
    Task<IReadOnlyList<Icd11Code>> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default);
}

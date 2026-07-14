using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IIcd10CodeRepository : IRepository<Icd10Code>
{
    Task<IReadOnlyList<Icd10Code>> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default);
}

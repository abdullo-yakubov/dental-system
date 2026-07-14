using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface ICurrencyRepository : IRepository<Currency>
{
    Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}

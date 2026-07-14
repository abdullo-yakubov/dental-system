using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class CurrencyRepository(DentalDbContext context) : RepositoryBase<Currency>(context), ICurrencyRepository
{
    public async Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .OrderByDescending(c => c.IsBase)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);

    public Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpper(), cancellationToken);
}

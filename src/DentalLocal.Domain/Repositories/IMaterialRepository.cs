using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IMaterialRepository : IRepository<Material>
{
    Task<IReadOnlyList<Material>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<Material>> GetLowStockAsync(CancellationToken cancellationToken = default);
}

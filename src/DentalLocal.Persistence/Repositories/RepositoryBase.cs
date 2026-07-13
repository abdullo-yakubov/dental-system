using DentalLocal.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public abstract class RepositoryBase<T>(DentalDbContext context) where T : Entity
{
    protected DentalDbContext Context { get; } = context;
    protected DbSet<T> Set { get; } = context.Set<T>();

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);
}

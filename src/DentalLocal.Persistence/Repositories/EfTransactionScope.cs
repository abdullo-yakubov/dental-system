using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace DentalLocal.Persistence.Repositories;

public class EfTransactionScope(IDbContextTransaction transaction) : ITransactionScope
{
    public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}

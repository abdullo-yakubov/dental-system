namespace DentalLocal.Application.Common;

/// <summary>
/// Service responsible for checking and bootstrapping the local portable PostgreSQL instance.
/// </summary>
public interface IPostgresBootstrapService
{
    Task BootstrapAsync(CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

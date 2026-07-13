using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IAppSettingRepository
{
    Task<AppSetting?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(string key, string? value, CancellationToken cancellationToken = default);
}

using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class AppSettingRepository(DentalDbContext context) : IAppSettingRepository
{
    public Task<AppSetting?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        context.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

    public async Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.AppSettings.AsNoTracking().ToListAsync(cancellationToken);

    public async Task UpsertAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        var existing = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (existing is null)
        {
            await context.AppSettings.AddAsync(AppSetting.Create(key, value), cancellationToken);
        }
        else
        {
            existing.SetValue(value);
        }
    }
}

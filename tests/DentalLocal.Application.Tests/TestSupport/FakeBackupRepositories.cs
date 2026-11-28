using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;

namespace DentalLocal.Application.Tests.TestSupport;

internal sealed class FakeBackupRecordRepository(List<BackupRecord> records) : IBackupRecordRepository
{
    public Task<BackupRecord?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(records.FirstOrDefault(x => x.Id == id));
    public Task AddAsync(BackupRecord entity, CancellationToken ct = default)
    {
        records.Add(entity);
        return Task.CompletedTask;
    }
    public Task<IReadOnlyList<BackupRecord>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BackupRecord>>(records);
    public Task<BackupRecord?> GetLatestAsync(CancellationToken ct = default) =>
        Task.FromResult(records.OrderByDescending(x => x.CreatedAt).FirstOrDefault());
    public Task<IReadOnlyList<BackupRecord>> GetOlderThanAsync(DateTimeOffset threshold, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BackupRecord>>(records.Where(x => x.CreatedAt < threshold).ToList());
}

internal sealed class FakeAppSettingRepository(List<AppSetting> settings) : IAppSettingRepository
{
    public Task<AppSetting?> GetAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(settings.FirstOrDefault(x => x.Key == key));
    public Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AppSetting>>(settings);
    public Task UpsertAsync(string key, string? value, CancellationToken ct = default)
    {
        var setting = settings.FirstOrDefault(x => x.Key == key);
        if (setting is null) settings.Add(AppSetting.Create(key, value));
        else setting.SetValue(value);
        return Task.CompletedTask;
    }
}

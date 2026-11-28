using DentalLocal.Application.Common;
using DentalLocal.Application.Features.Backups;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using Xunit;

namespace DentalLocal.Application.Tests.Features.Backups;

public class BackupHandlersTests
{
    [Fact]
    public async Task Restore_rejects_unknown_file_without_enqueuing_job()
    {
        var uow = new FakeUnitOfWork();
        uow.AppSettingsData.Add(AppSetting.Create(AppSettingKeys.AccessCodeHash, PasswordHasher.Hash("12345678")));
        var queue = new FakeQueue();

        var result = await new RestoreBackupCommandHandler(uow, queue)
            .Handle(new RestoreBackupCommand("12345678", "../../other.backup"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Backup.NotFound", result.Error.Code);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public async Task Restore_rejects_wrong_access_code()
    {
        var uow = new FakeUnitOfWork();
        uow.AppSettingsData.Add(AppSetting.Create(AppSettingKeys.AccessCodeHash, PasswordHasher.Hash("12345678")));
        uow.BackupRecordsData.Add(BackupRecord.Create("sample.backup", "Backups/manual/sample.backup", 100,
            BackupTrigger.Manual, true, null));
        var queue = new FakeQueue();

        var result = await new RestoreBackupCommandHandler(uow, queue)
            .Handle(new RestoreBackupCommand("wrong", "sample.backup"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Backup.InvalidCode", result.Error.Code);
        Assert.Equal(0, queue.Count);
    }

    private sealed class FakeQueue : IJobQueue
    {
        public int Count { get; private set; }
        public Guid Enqueue(string jobType, Func<Guid, IServiceProvider, Action<int>, CancellationToken, Task> work)
        {
            Count++;
            return Guid.NewGuid();
        }
        public JobRecord? GetJob(Guid id) => null;
        public IReadOnlyList<JobRecord> GetJobs(JobStatus? status) => [];
    }
}

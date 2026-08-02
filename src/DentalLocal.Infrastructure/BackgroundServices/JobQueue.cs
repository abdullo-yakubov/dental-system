using System.Collections.Concurrent;
using System.Threading.Channels;
using DentalLocal.Application.Common;

namespace DentalLocal.Infrastructure.BackgroundServices;

internal sealed record JobWorkItem(Guid Id, string Type, Func<Guid, IServiceProvider, Action<int>, CancellationToken, Task> Work);

/// <summary>In-memory очередь фоновых задач: singleton, читается единственным JobRunnerBackgroundService.</summary>
internal sealed class JobQueue : IJobQueue
{
    private readonly Channel<JobWorkItem> _channel = Channel.CreateUnbounded<JobWorkItem>();
    private readonly ConcurrentDictionary<Guid, JobRecord> _records = new();

    public ChannelReader<JobWorkItem> Reader => _channel.Reader;

    public Guid Enqueue(string jobType, Func<Guid, IServiceProvider, Action<int>, CancellationToken, Task> work)
    {
        var id = Guid.NewGuid();
        var record = new JobRecord { Id = id, Type = jobType, CreatedAt = DateTime.UtcNow };
        _records[id] = record;
        _channel.Writer.TryWrite(new JobWorkItem(id, jobType, work));
        return id;
    }

    public JobRecord? GetJob(Guid id) => _records.GetValueOrDefault(id);

    public IReadOnlyList<JobRecord> GetJobs(JobStatus? status) =>
        _records.Values
            .Where(r => status is null || r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

    public void UpdateStatus(Guid id, JobStatus status, string? error = null)
    {
        if (!_records.TryGetValue(id, out var record)) return;

        record.Status = status;
        record.Error = error;
        if (status is JobStatus.Succeeded or JobStatus.Failed)
            record.FinishedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(Guid id, int percent)
    {
        if (_records.TryGetValue(id, out var record))
            record.Progress = percent;
    }
}

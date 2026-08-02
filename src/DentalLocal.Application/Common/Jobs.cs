namespace DentalLocal.Application.Common;

public enum JobStatus
{
    Pending,
    Running,
    Succeeded,
    Failed
}

/// <summary>Статус долгоживущей фоновой операции (бэкап, восстановление, экспорт), не привязанной к HTTP-запросу.</summary>
public sealed class JobRecord
{
    public required Guid Id { get; init; }
    public required string Type { get; init; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public int Progress { get; set; }
    public string? Error { get; set; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? FinishedAt { get; set; }
}

/// <summary>
/// Лёгкий job-раннер для операций, которые не должны жить внутри HTTP-запроса (backup/restore,
/// тяжёлые экспорты). Job переживает закрытие вкладки браузера — клиент получает jobId и опрашивает
/// статус отдельно. Реализация — Infrastructure/BackgroundServices/JobQueue.cs.
/// </summary>
public interface IJobQueue
{
    /// <param name="work">Тело задачи: (jobId, scoped IServiceProvider, callback прогресса 0-100, CancellationToken) -> Task.
    /// jobId нужен, например, чтобы джоба сохранила бинарный результат в IJobResultStore под собственным ключом.</param>
    Guid Enqueue(string jobType, Func<Guid, IServiceProvider, Action<int>, CancellationToken, Task> work);

    JobRecord? GetJob(Guid id);

    IReadOnlyList<JobRecord> GetJobs(JobStatus? status);
}

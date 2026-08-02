using DentalLocal.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DentalLocal.Infrastructure.BackgroundServices;

/// <summary>
/// Единственный воркер, последовательно выполняющий джобы из JobQueue вне HTTP-запроса
/// (backup/restore, тяжёлые экспорты) — см. промпт 5/6 аудита. Каждая задача выполняется
/// в собственном DI-scope, чтобы не делить DbContext между задачами.
/// </summary>
internal sealed class JobRunnerBackgroundService(
    JobQueue queue,
    IServiceProvider rootProvider,
    ILogger<JobRunnerBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in queue.Reader.ReadAllAsync(stoppingToken))
        {
            queue.UpdateStatus(item.Id, JobStatus.Running);
            logger.LogInformation("Джоба {JobId} ({JobType}) запущена", item.Id, item.Type);

            using var scope = rootProvider.CreateScope();
            try
            {
                await item.Work(item.Id, scope.ServiceProvider, percent => queue.UpdateProgress(item.Id, percent), stoppingToken);
                queue.UpdateStatus(item.Id, JobStatus.Succeeded);
                logger.LogInformation("Джоба {JobId} ({JobType}) завершена успешно", item.Id, item.Type);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Джоба {JobId} ({JobType}) завершилась с ошибкой", item.Id, item.Type);
                queue.UpdateStatus(item.Id, JobStatus.Failed, ex.Message);
            }
        }
    }
}

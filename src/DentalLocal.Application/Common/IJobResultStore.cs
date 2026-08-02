namespace DentalLocal.Application.Common;

/// <summary>
/// Хранилище бинарных результатов джоб (см. Jobs.cs) — например, файл Excel-экспорта, сгенерированный
/// вне HTTP-запроса (см. промпт 47 аудита: большие экспорты не должны генерироваться синхронно внутри
/// запроса). Джоба кладёт готовый файл сюда, клиент забирает его отдельным GET после того, как job
/// перешла в Succeeded.
/// </summary>
public interface IJobResultStore
{
    void Set(Guid jobId, byte[] content, string contentType, string fileName, TimeSpan ttl);

    bool TryGet(Guid jobId, out byte[] content, out string contentType, out string fileName);
}

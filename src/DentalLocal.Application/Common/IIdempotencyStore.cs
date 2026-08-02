namespace DentalLocal.Application.Common;

/// <summary>
/// Хранилище результатов финансовых POST-запросов по клиентскому Idempotency-Key (см. промпт 17
/// аудита): двойной клик по «Внести оплату» отправляет два одинаковых запроса с одним и тем же
/// ключом — второй должен получить тот же результат, а не создать второй платёж.
/// </summary>
public interface IIdempotencyStore
{
    bool TryGet(string key, out string cachedJson);

    void Set(string key, string json, TimeSpan ttl);
}

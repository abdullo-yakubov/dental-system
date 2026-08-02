namespace DentalLocal.Application.Common;

/// <summary>
/// Защита от перебора кода доступа: 5 неверных попыток → блокировка, с экспоненциальным ростом
/// при повторных сериях (30с → 2м → 10м) — см. промпт 15 аудита.
/// </summary>
public interface ILoginAttemptTracker
{
    bool IsLockedOut(out int remainingSeconds);

    void RegisterFailure();

    void Reset();
}

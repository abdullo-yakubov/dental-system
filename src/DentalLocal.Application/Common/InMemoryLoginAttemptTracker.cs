using DentalLocal.Shared.Common;

namespace DentalLocal.Application.Common;

/// <summary>
/// Простой процессный трекер попыток входа (см. <see cref="ILoginAttemptTracker"/>).
/// Состояние не переживает перезапуск backend — этого достаточно для однопользовательской локальной системы.
/// </summary>
public sealed class InMemoryLoginAttemptTracker : ILoginAttemptTracker
{
    // Экспоненциальный рост при повторных сериях блокировок (см. промпт 15 аудита): первая серия из
    // MaxLoginAttempts неверных попыток блокирует на 30с, следующая (если снова набрали лимит,
    // не залогинившись успешно между ними) — на 2м, далее — на 10м, дальше рост не продолжается.
    private static readonly TimeSpan[] LockoutStages = [TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10)];

    private readonly object _lock = new();
    private int _failureCount;
    private int _lockoutStageIndex;
    private DateTimeOffset? _lockedUntil;

    public bool IsLockedOut(out int remainingSeconds)
    {
        lock (_lock)
        {
            if (_lockedUntil is null || _lockedUntil <= DateTimeOffset.UtcNow)
            {
                remainingSeconds = 0;
                return false;
            }

            remainingSeconds = (int)Math.Ceiling((_lockedUntil.Value - DateTimeOffset.UtcNow).TotalSeconds);
            return true;
        }
    }

    public void RegisterFailure()
    {
        lock (_lock)
        {
            _failureCount++;

            if (_failureCount >= DentalConstants.MaxLoginAttempts)
            {
                var stage = Math.Min(_lockoutStageIndex, LockoutStages.Length - 1);
                _lockedUntil = DateTimeOffset.UtcNow.Add(LockoutStages[stage]);
                _failureCount = 0;
                _lockoutStageIndex = Math.Min(_lockoutStageIndex + 1, LockoutStages.Length - 1);
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _lockedUntil = null;
            _lockoutStageIndex = 0;
        }
    }
}

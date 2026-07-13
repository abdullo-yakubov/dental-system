using DentalLocal.Shared.Common;

namespace DentalLocal.Application.Common;

/// <summary>
/// Простой процессный трекер попыток входа (см. <see cref="ILoginAttemptTracker"/>).
/// Состояние не переживает перезапуск backend — этого достаточно для однопользовательской локальной системы.
/// </summary>
public sealed class InMemoryLoginAttemptTracker : ILoginAttemptTracker
{
    private readonly object _lock = new();
    private int _failureCount;
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
                _lockedUntil = DateTimeOffset.UtcNow.AddSeconds(DentalConstants.LoginLockoutSeconds);
                _failureCount = 0;
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _lockedUntil = null;
        }
    }
}

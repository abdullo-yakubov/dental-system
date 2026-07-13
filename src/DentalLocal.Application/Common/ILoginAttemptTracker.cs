namespace DentalLocal.Application.Common;

/// <summary>Защита от перебора кода доступа: 5 неверных попыток → блокировка на 30 сек (п.11, 17 ТЗ).</summary>
public interface ILoginAttemptTracker
{
    bool IsLockedOut(out int remainingSeconds);

    void RegisterFailure();

    void Reset();
}

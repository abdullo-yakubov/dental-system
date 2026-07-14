namespace DentalLocal.Client.Services;

/// <summary>
/// Состояние блокировки экрана по простою (раздел 11 ТЗ: AutoLockMinutes бездействия).
/// Сама разблокировка выполняется повторным вводом PIN через тот же /auth/login эндпоинт —
/// сервис лишь хранит признак "заблокировано", не дублирует логику проверки кода.
/// </summary>
public sealed class SessionLockService
{
    public bool IsLocked { get; private set; }

    public event Action? Changed;

    public void Lock()
    {
        if (IsLocked)
            return;

        IsLocked = true;
        Changed?.Invoke();
    }

    public void Unlock()
    {
        if (!IsLocked)
            return;

        IsLocked = false;
        Changed?.Invoke();
    }
}

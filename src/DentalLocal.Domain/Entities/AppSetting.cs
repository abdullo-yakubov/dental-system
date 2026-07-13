using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

/// <summary>Настройка приложения в формате ключ/значение (код доступа, имя клиники, валюта, тема и т.д.).</summary>
public class AppSetting : Entity
{
    public string Key { get; private set; } = null!;
    public string? Value { get; private set; }

    private AppSetting()
    {
    }

    public static AppSetting Create(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Ключ настройки обязателен.", nameof(key));

        return new AppSetting
        {
            Key = key,
            Value = value,
        };
    }

    public void SetValue(string? value)
    {
        Value = value;
        Touch();
    }
}

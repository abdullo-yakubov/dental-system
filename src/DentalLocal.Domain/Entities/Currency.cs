using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class Currency : Entity
{
    public string Code { get; private set; } = null!; // e.g. "USD", "UZS"
    public string Name { get; private set; } = null!;
    public string Symbol { get; private set; } = null!;
    public decimal ExchangeRateToBase { get; private set; }
    public bool IsBase { get; private set; }

    private Currency()
    {
    }

    public static Currency Create(string code, string name, string symbol, decimal exchangeRateToBase, bool isBase)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Код валюты обязателен.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название валюты обязательно.", nameof(name));
        if (exchangeRateToBase <= 0)
            throw new ArgumentException("Курс обмена должен быть больше нуля.", nameof(exchangeRateToBase));

        return new Currency
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Symbol = symbol.Trim(),
            ExchangeRateToBase = exchangeRateToBase,
            IsBase = isBase
        };
    }

    public void Update(string name, string symbol, decimal exchangeRateToBase, bool isBase)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название валюты обязательно.", nameof(name));
        if (exchangeRateToBase <= 0)
            throw new ArgumentException("Курс обмена должен быть больше нуля.", nameof(exchangeRateToBase));

        Name = name.Trim();
        Symbol = symbol.Trim();
        ExchangeRateToBase = exchangeRateToBase;
        IsBase = isBase;
        Touch();
    }
}

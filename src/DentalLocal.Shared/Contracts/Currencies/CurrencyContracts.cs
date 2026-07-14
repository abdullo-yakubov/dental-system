namespace DentalLocal.Shared.Contracts.Currencies;

public sealed record CurrencyDto(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBase);

public sealed record CreateCurrencyRequest(
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBase);

public sealed record UpdateCurrencyRequest(
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBase);

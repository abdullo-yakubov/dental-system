using DentalLocal.Application.Features.Currencies;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;

namespace DentalLocal.Application.Tests.Features.Currencies;

public class CreateCurrencyCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesCurrency()
    {
        var uow = new FakeUnitOfWork();
        var handler = new CreateCurrencyCommandHandler(uow);
        var command = new CreateCurrencyCommand("USD", "Доллар США", "$", 12_500m, false);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var currency = Assert.Single(uow.CurrenciesData);
        Assert.Equal("USD", currency.Code);
    }

    [Fact]
    public async Task Handle_DuplicateCode_ReturnsValidationErrorAndDoesNotAddSecondCurrency()
    {
        var uow = new FakeUnitOfWork();
        uow.CurrenciesData.Add(Currency.Create("USD", "Доллар США", "$", 12_500m, false));

        var handler = new CreateCurrencyCommandHandler(uow);
        var command = new CreateCurrencyCommand("USD", "Доллар США (повтор)", "$", 12_600m, false);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Currency.Duplicate", result.Error.Code);
        Assert.Single(uow.CurrenciesData);
    }
}

public class UpdateCurrencyCommandHandlerTests
{
    [Fact]
    public async Task Handle_ZeroExchangeRate_ThrowsValidatedAsInvalidData()
    {
        var uow = new FakeUnitOfWork();
        var currency = Currency.Create("EUR", "Евро", "€", 13_500m, false);
        uow.CurrenciesData.Add(currency);

        var handler = new UpdateCurrencyCommandHandler(uow);
        var command = new UpdateCurrencyCommand(currency.Id, "Евро", "€", 0m, false);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Currency.InvalidData", result.Error.Code);
        Assert.Equal(13_500m, currency.ExchangeRateToBase);
    }

    [Fact]
    public async Task Handle_CurrencyNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var handler = new UpdateCurrencyCommandHandler(uow);
        var command = new UpdateCurrencyCommand(Guid.NewGuid(), "Евро", "€", 13_500m, false);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Currency.NotFound", result.Error.Code);
    }
}

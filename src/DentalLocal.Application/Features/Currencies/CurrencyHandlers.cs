using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Currencies;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Currencies;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetCurrenciesQuery : IQuery<IReadOnlyList<CurrencyDto>>;

public sealed class GetCurrenciesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCurrenciesQuery, Result<IReadOnlyList<CurrencyDto>>>
{
    public async Task<Result<IReadOnlyList<CurrencyDto>>> Handle(GetCurrenciesQuery request, CancellationToken cancellationToken)
    {
        var currencies = await unitOfWork.Currencies.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CurrencyDto>>(currencies.Select(ToDto).ToList());
    }

    internal static CurrencyDto ToDto(Currency c) =>
        new(c.Id, c.Code, c.Name, c.Symbol, c.ExchangeRateToBase, c.IsBase);
}

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record CreateCurrencyCommand(
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBase) : ICommand<CurrencyDto>;

public sealed class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10).WithMessage("Укажите код валюты.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage("Укажите название валюты.");
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10).WithMessage("Укажите символ валюты.");
        RuleFor(x => x.ExchangeRateToBase).GreaterThan(0).WithMessage("Курс должен быть больше нуля.");
    }
}

public sealed class CreateCurrencyCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCurrencyCommand, Result<CurrencyDto>>
{
    public async Task<Result<CurrencyDto>> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.Currencies.GetByCodeAsync(request.Code, cancellationToken);
        if (existing is not null)
            return Result.Failure<CurrencyDto>(Error.Validation("Currency.Duplicate", $"Валюта с кодом {request.Code} уже существует."));

        Currency currency;
        try
        {
            currency = Currency.Create(request.Code, request.Name, request.Symbol, request.ExchangeRateToBase, request.IsBase);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CurrencyDto>(Error.Validation("Currency.InvalidData", ex.Message));
        }

        await unitOfWork.Currencies.AddAsync(currency, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("CURRENCY_CREATED", "Currency", currency.Id, null), cancellationToken);

        return Result.Success(GetCurrenciesQueryHandler.ToDto(currency));
    }
}

// ---------------------------------------------------------------------------

public sealed record UpdateCurrencyCommand(
    Guid Id,
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBase) : ICommand<CurrencyDto>;

public sealed class UpdateCurrencyCommandValidator : AbstractValidator<UpdateCurrencyCommand>
{
    public UpdateCurrencyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(10);
        RuleFor(x => x.ExchangeRateToBase).GreaterThan(0);
    }
}

public sealed class UpdateCurrencyCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCurrencyCommand, Result<CurrencyDto>>
{
    public async Task<Result<CurrencyDto>> Handle(UpdateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var currency = await unitOfWork.Currencies.GetByIdAsync(request.Id, cancellationToken);
        if (currency is null)
            return Result.Failure<CurrencyDto>(Error.NotFound("Currency.NotFound", "Валюта не найдена."));

        try
        {
            currency.Update(request.Name, request.Symbol, request.ExchangeRateToBase, request.IsBase);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CurrencyDto>(Error.Validation("Currency.InvalidData", ex.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("CURRENCY_UPDATED", "Currency", currency.Id, null), cancellationToken);

        return Result.Success(GetCurrenciesQueryHandler.ToDto(currency));
    }
}

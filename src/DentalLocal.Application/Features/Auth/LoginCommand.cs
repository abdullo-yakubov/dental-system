using DentalLocal.Application.Common;
using DentalLocal.Domain.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Auth;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Auth;

/// <summary>
/// Реализован как <see cref="IQuery{TResponse}"/>, а не команда: не изменяет доменные сущности через EF
/// (только читает код доступа и пишет запись аудита напрямую), поэтому не нуждается в оборачивании
/// <c>TransactionBehavior</c> в транзакцию — иначе аудит неудачной попытки входа откатывался бы вместе
/// с общим результатом Failure.
/// </summary>
public sealed record LoginCommand(string Code) : IQuery<LoginResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Введите код доступа.");
    }
}

public sealed class LoginCommandHandler(
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    ILoginAttemptTracker attemptTracker) : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (attemptTracker.IsLockedOut(out var remainingSeconds))
        {
            return Result.Failure<LoginResponse>(
                Error.Forbidden("Auth.LockedOut", $"Слишком много неверных попыток. Повторите через {remainingSeconds} сек."));
        }

        var setting = await unitOfWork.AppSettings.GetAsync(AppSettingKeys.AccessCodeHash, cancellationToken);
        var isValid = setting?.Value is not null && PasswordHasher.Verify(request.Code, setting.Value);

        if (!isValid)
        {
            attemptTracker.RegisterFailure();
            await unitOfWork.AuditLogs.AddAsync(AuditLog.Create("LOGIN_FAIL", null, null, null), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<LoginResponse>(Error.Validation("Auth.InvalidCode", "Неверный код доступа."));
        }

        attemptTracker.Reset();

        var (token, expiresAt) = await tokenService.IssueAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(AuditLog.Create("LOGIN_OK", null, null, null), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(token, expiresAt);
    }
}

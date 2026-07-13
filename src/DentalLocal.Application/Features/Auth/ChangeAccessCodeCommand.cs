using DentalLocal.Application.Common;
using DentalLocal.Domain.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Common;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Auth;

public sealed record ChangeAccessCodeCommand(string OldCode, string NewCode) : ICommand;

public sealed class ChangeAccessCodeCommandValidator : AbstractValidator<ChangeAccessCodeCommand>
{
    public ChangeAccessCodeCommandValidator()
    {
        RuleFor(x => x.OldCode).NotEmpty().WithMessage("Введите текущий код доступа.");
        RuleFor(x => x.NewCode)
            .NotEmpty().WithMessage("Введите новый код доступа.")
            .MinimumLength(DentalConstants.MinAccessCodeLength)
            .WithMessage($"Новый код доступа должен содержать не менее {DentalConstants.MinAccessCodeLength} символов.");
    }
}

public sealed class ChangeAccessCodeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ChangeAccessCodeCommand, Result>
{
    public async Task<Result> Handle(ChangeAccessCodeCommand request, CancellationToken cancellationToken)
    {
        var setting = await unitOfWork.AppSettings.GetAsync(AppSettingKeys.AccessCodeHash, cancellationToken);

        if (setting?.Value is null || !PasswordHasher.Verify(request.OldCode, setting.Value))
            return Result.Failure(Error.Validation("Auth.InvalidCode", "Текущий код доступа введён неверно."));

        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.AccessCodeHash, PasswordHasher.Hash(request.NewCode), cancellationToken);
        await unitOfWork.AuditLogs.AddAsync(AuditLog.Create("SETTINGS_CHANGED", "AppSetting", null, "{\"key\":\"access_code_hash\"}"), cancellationToken);

        return Result.Success();
    }
}

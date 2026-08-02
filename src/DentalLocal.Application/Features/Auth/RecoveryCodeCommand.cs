using System.Security.Cryptography;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Common;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Auth;

// Генерация recovery-кода — требует текущий PIN (эндпоинт защищён Bearer-токеном, т.е. пользователь
// уже вошёл в систему). Код показывается ровно один раз, хранится только его хэш. Генерация нового
// кода инвалидирует предыдущий.
public sealed record GenerateRecoveryCodeCommand(string CurrentPin) : IRequest<Result<string>>;

public sealed class GenerateRecoveryCodeCommandValidator : AbstractValidator<GenerateRecoveryCodeCommand>
{
    public GenerateRecoveryCodeCommandValidator()
    {
        RuleFor(x => x.CurrentPin).NotEmpty().WithMessage("Введите текущий код доступа.");
    }
}

public sealed class GenerateRecoveryCodeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<GenerateRecoveryCodeCommand, Result<string>>
{
    // 8 групп по 4 символа из алфавита Crockford Base32 без неоднозначных символов (0/O, 1/I/L)
    // — код диктуют по телефону или переписывают от руки без ошибок.
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTVWXYZ";

    public async Task<Result<string>> Handle(GenerateRecoveryCodeCommand request, CancellationToken cancellationToken)
    {
        var setting = await unitOfWork.AppSettings.GetAsync(AppSettingKeys.AccessCodeHash, cancellationToken);
        if (setting?.Value is null || !PasswordHasher.Verify(request.CurrentPin, setting.Value))
            return Result.Failure<string>(Error.Validation("Auth.InvalidCode", "Текущий код доступа введён неверно."));

        var recoveryCode = GenerateCode();

        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.RecoveryCodeHash, PasswordHasher.Hash(recoveryCode), cancellationToken);
        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("SECURITY_RECOVERY_CODE_GENERATED", "AppSetting", null, "{\"key\":\"recovery_code_hash\"}"),
            cancellationToken);

        return Result.Success(recoveryCode);
    }

    private static string GenerateCode()
    {
        var groups = new string[8];
        for (var i = 0; i < groups.Length; i++)
        {
            var chars = new char[4];
            for (var j = 0; j < chars.Length; j++)
            {
                chars[j] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }
            groups[i] = new string(chars);
        }
        return string.Join('-', groups);
    }
}

// Восстановление доступа по recovery-коду — единственный путь, доступный без Bearer-токена
// (пользователь по определению не может войти). После успешного восстановления recovery-код
// инвалидируется: чтобы получить новый, нужно сначала войти новым PIN и сгенерировать его заново.
public sealed record ResetAccessCodeWithRecoveryCommand(string RecoveryCode, string NewCode) : IRequest<Result>;

public sealed class ResetAccessCodeWithRecoveryCommandValidator : AbstractValidator<ResetAccessCodeWithRecoveryCommand>
{
    public ResetAccessCodeWithRecoveryCommandValidator()
    {
        RuleFor(x => x.RecoveryCode).NotEmpty().WithMessage("Введите код восстановления.");
        RuleFor(x => x.NewCode)
            .NotEmpty().WithMessage("Введите новый код доступа.")
            .MinimumLength(DentalConstants.MinAccessCodeLength)
            .WithMessage($"Новый код доступа должен содержать не менее {DentalConstants.MinAccessCodeLength} символов.");
    }
}

public sealed class ResetAccessCodeWithRecoveryCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ResetAccessCodeWithRecoveryCommand, Result>
{
    public async Task<Result> Handle(ResetAccessCodeWithRecoveryCommand request, CancellationToken cancellationToken)
    {
        var setting = await unitOfWork.AppSettings.GetAsync(AppSettingKeys.RecoveryCodeHash, cancellationToken);
        if (setting?.Value is null || !PasswordHasher.Verify(request.RecoveryCode, setting.Value))
        {
            await unitOfWork.AuditLogs.AddAsync(
                AuditLog.Create("SECURITY_RECOVERY_CODE_FAILED", "AppSetting", null, null),
                cancellationToken);
            return Result.Failure(Error.Validation("Auth.InvalidRecoveryCode", "Неверный код восстановления."));
        }

        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.AccessCodeHash, PasswordHasher.Hash(request.NewCode), cancellationToken);
        // Код одноразовый: делаем текущий хэш недостижимым для повторного использования.
        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.RecoveryCodeHash, PasswordHasher.Hash(Guid.NewGuid().ToString("N")), cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("SECURITY_ACCESS_CODE_RESET_VIA_RECOVERY", "AppSetting", null, "{\"key\":\"access_code_hash\"}"),
            cancellationToken);

        return Result.Success();
    }
}

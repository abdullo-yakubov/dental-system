using DentalLocal.Application.Common;
using DentalLocal.Domain.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Settings;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace DentalLocal.Application.Features.Settings;

// 1. Get Settings Query
public sealed record GetSettingsQuery : IQuery<SettingsDto>;

public sealed class GetSettingsQueryHandler(IUnitOfWork unitOfWork, IMemoryCache cache) : IRequestHandler<GetSettingsQuery, Result<SettingsDto>>
{
    public async Task<Result<SettingsDto>> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        var dto = await cache.GetOrCreateAsync(CacheKeys.Settings, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

            var settings = (await unitOfWork.AppSettings.GetAllAsync(cancellationToken))
                .ToDictionary(x => x.Key, x => x.Value);

            string Get(string key, string fallback) => settings.TryGetValue(key, out var value) && value is not null ? value : fallback;

            return new SettingsDto(
                Get(AppSettingKeys.DoctorName, "Врач-администратор"),
                Get(AppSettingKeys.ClinicName, "DentalLocal"),
                Get(AppSettingKeys.Currency, DentalDefaults.Currency),
                Get(AppSettingKeys.Theme, "system"),
                Get(AppSettingKeys.Language, "ru"),
                Get(AppSettingKeys.BackupTime, DentalDefaults.BackupTime),
                int.TryParse(Get(AppSettingKeys.BackupRetentionDays, DentalDefaults.BackupRetentionDays.ToString()), out var days)
                    ? days
                    : DentalDefaults.BackupRetentionDays,
                int.TryParse(Get(AppSettingKeys.AutoLockMinutes, DentalDefaults.AutoLockMinutes.ToString()), out var lockMinutes)
                    ? lockMinutes
                    : DentalDefaults.AutoLockMinutes,
                bool.TryParse(Get(AppSettingKeys.Enable3DOdontogram, "false"), out var enable3D) && enable3D);
        });

        return Result.Success(dto!);
    }
}

// 2. Update Settings Command
public sealed record UpdateSettingsCommand(
    string DoctorName,
    string ClinicName,
    string Currency,
    string Theme,
    string Language,
    string BackupTime,
    int BackupRetentionDays,
    int AutoLockMinutes,
    bool Enable3DOdontogram) : ICommand<SettingsDto>;

public sealed class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsCommandValidator()
    {
        RuleFor(x => x.DoctorName).NotEmpty().WithMessage("Укажите ФИО врача.");
        RuleFor(x => x.ClinicName).NotEmpty().WithMessage("Укажите название клиники.");
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Укажите валюту.");
        RuleFor(x => x.BackupRetentionDays).GreaterThan(0).WithMessage("Срок хранения бэкапов должен быть положительным.");
        RuleFor(x => x.AutoLockMinutes).GreaterThan(0).WithMessage("Время автоблокировки должно быть положительным.");
    }
}

public sealed class UpdateSettingsCommandHandler(IUnitOfWork unitOfWork, IMemoryCache cache) : IRequestHandler<UpdateSettingsCommand, Result<SettingsDto>>
{
    public async Task<Result<SettingsDto>> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.DoctorName, request.DoctorName, cancellationToken);
        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.ClinicName, request.ClinicName, cancellationToken);
        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.Currency, request.Currency, cancellationToken);
        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.Theme, request.Theme, cancellationToken);
        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.Language, request.Language, cancellationToken);
        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.BackupTime, request.BackupTime, cancellationToken);
        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.BackupRetentionDays, request.BackupRetentionDays.ToString(), cancellationToken);
        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.AutoLockMinutes, request.AutoLockMinutes.ToString(), cancellationToken);
        await unitOfWork.AppSettings.UpsertAsync(AppSettingKeys.Enable3DOdontogram, request.Enable3DOdontogram.ToString(), cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(AuditLog.Create("SETTINGS_CHANGED", "AppSetting", null, null), cancellationToken);

        cache.Remove(CacheKeys.Settings);

        var dto = new SettingsDto(
            request.DoctorName, request.ClinicName, request.Currency,
            request.Theme, request.Language, request.BackupTime, request.BackupRetentionDays, request.AutoLockMinutes,
            request.Enable3DOdontogram);

        return Result.Success(dto);
    }
}

using DentalLocal.Application.Common;
using DentalLocal.Domain.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Backups;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DentalLocal.Application.Features.Backups;

public sealed record GetBackupsListQuery : IQuery<IReadOnlyList<BackupDto>>;

public sealed class GetBackupsListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetBackupsListQuery, Result<IReadOnlyList<BackupDto>>>
{
    public async Task<Result<IReadOnlyList<BackupDto>>> Handle(GetBackupsListQuery request, CancellationToken ct)
    {
        var records = await unitOfWork.BackupRecords.GetAllAsync(ct);
        return records.Select(x => new BackupDto(x.FileName, x.CreatedAt, x.SizeBytes,
            (short)x.Trigger, x.IsValid, x.Note)).ToList();
    }
}

public sealed record CreateManualBackupCommand(string Pin, string? Note) : IQuery<Guid>;

public sealed class CreateManualBackupCommandHandler(IUnitOfWork unitOfWork, IJobQueue queue)
    : IRequestHandler<CreateManualBackupCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateManualBackupCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Pin))
            return Result.Failure<Guid>(Error.Validation("Backup.PinRequired", "Введите код доступа."));
        var setting = await unitOfWork.AppSettings.GetAsync(AppSettingKeys.AccessCodeHash, ct);
        if (setting?.Value is null || !PasswordHasher.Verify(request.Pin, setting.Value))
            return Result.Failure<Guid>(Error.Forbidden("Backup.InvalidCode", "Неверный код доступа."));

        var id = queue.Enqueue("backup.create", async (_, services, _, token) =>
            await services.GetRequiredService<IBackupService>().CreateBackupAsync(request.Pin, request.Note, token));
        return id;
    }
}

public sealed record RestoreBackupCommand(string Pin, string BackupFileName) : IQuery<Guid>;

public sealed class RestoreBackupCommandHandler(IUnitOfWork unitOfWork, IJobQueue queue)
    : IRequestHandler<RestoreBackupCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RestoreBackupCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Pin))
            return Result.Failure<Guid>(Error.Validation("Backup.PinRequired", "Введите код доступа."));
        var setting = await unitOfWork.AppSettings.GetAsync(AppSettingKeys.AccessCodeHash, ct);
        if (setting?.Value is null || !PasswordHasher.Verify(request.Pin, setting.Value))
            return Result.Failure<Guid>(Error.Forbidden("Backup.InvalidCode", "Неверный код доступа."));

        var record = (await unitOfWork.BackupRecords.GetAllAsync(ct))
            .FirstOrDefault(x => x.FileName == request.BackupFileName);
        if (record is null)
            return Result.Failure<Guid>(Error.NotFound("Backup.NotFound", "Копия не найдена."));

        var id = queue.Enqueue("backup.restore", async (_, services, _, token) =>
            await services.GetRequiredService<IBackupService>().RestoreBackupAsync(request.Pin, record.FilePath, token));
        return id;
    }
}

public sealed record ValidateBackupIntegrityCommand(string FileName) : IQuery<bool>;

public sealed class ValidateBackupIntegrityCommandHandler(IUnitOfWork unitOfWork, IBackupService backupService)
    : IRequestHandler<ValidateBackupIntegrityCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ValidateBackupIntegrityCommand request, CancellationToken ct)
    {
        var record = (await unitOfWork.BackupRecords.GetAllAsync(ct))
            .FirstOrDefault(x => x.FileName == request.FileName);
        if (record is null)
            return Result.Failure<bool>(Error.NotFound("Backup.NotFound", "Копия не найдена."));
        return await backupService.ValidateBackupIntegrityAsync(record.FilePath, ct);
    }
}

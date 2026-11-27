using DentalLocal.Application.Common;
using DentalLocal.Domain.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Backups;
using FluentValidation;
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
        return Result.Success<IReadOnlyList<BackupDto>>(records.Select(r =>
            new BackupDto(r.Id, r.FileName, r.SizeBytes, (short)r.Trigger, r.IsValid, r.Note, r.CreatedAt)).ToList());
    }
}

public sealed record CreateManualBackupCommand(string Pin, string? Note) : IRequest<Result<Guid>>;

public sealed class CreateManualBackupCommandValidator : AbstractValidator<CreateManualBackupCommand>
{
    public CreateManualBackupCommandValidator() => RuleFor(x => x.Pin).NotEmpty();
}

public sealed class CreateManualBackupCommandHandler(IUnitOfWork unitOfWork, IJobQueue jobs)
    : IRequestHandler<CreateManualBackupCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateManualBackupCommand request, CancellationToken ct)
    {
        var setting = await unitOfWork.AppSettings.GetAsync(AppSettingKeys.AccessCodeHash, ct);
        if (setting?.Value is null || !PasswordHasher.Verify(request.Pin, setting.Value))
            return Result.Failure<Guid>(Error.Validation("Auth.InvalidCode", "Неверный код доступа."));

        var jobId = jobs.Enqueue("backup.create", async (_, services, _, jobCt) =>
        {
            var backup = services.GetRequiredService<IBackupService>();
            await backup.CreateBackupAsync(request.Pin, request.Note, jobCt);
        });
        return Result.Success(jobId);
    }
}

public sealed record RestoreBackupCommand(string Pin, string BackupFileName) : IRequest<Result<Guid>>;

public sealed class RestoreBackupCommandValidator : AbstractValidator<RestoreBackupCommand>
{
    public RestoreBackupCommandValidator()
    {
        RuleFor(x => x.Pin).NotEmpty();
        RuleFor(x => x.BackupFileName).NotEmpty();
    }
}

public sealed class RestoreBackupCommandHandler(IUnitOfWork unitOfWork, IJobQueue jobs)
    : IRequestHandler<RestoreBackupCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RestoreBackupCommand request, CancellationToken ct)
    {
        var setting = await unitOfWork.AppSettings.GetAsync(AppSettingKeys.AccessCodeHash, ct);
        if (setting?.Value is null || !PasswordHasher.Verify(request.Pin, setting.Value))
            return Result.Failure<Guid>(Error.Validation("Auth.InvalidCode", "Неверный код доступа."));

        var records = await unitOfWork.BackupRecords.GetAllAsync(ct);
        var record = records.FirstOrDefault(r => r.FileName == request.BackupFileName);
        if (record is null)
            return Result.Failure<Guid>(Error.NotFound("Backup.NotFound", "Резервная копия не найдена."));

        var jobId = jobs.Enqueue("backup.restore", async (_, services, _, jobCt) =>
        {
            var backup = services.GetRequiredService<IBackupService>();
            await backup.RestoreBackupAsync(request.Pin, record.FilePath, jobCt);
        });
        return Result.Success(jobId);
    }
}

public sealed record ValidateBackupIntegrityCommand(string FileName) : IQuery<bool>;

public sealed class ValidateBackupIntegrityCommandHandler(IUnitOfWork unitOfWork, IBackupService backup)
    : IRequestHandler<ValidateBackupIntegrityCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ValidateBackupIntegrityCommand request, CancellationToken ct)
    {
        var records = await unitOfWork.BackupRecords.GetAllAsync(ct);
        var record = records.FirstOrDefault(r => r.FileName == request.FileName);
        if (record is null)
            return Result.Failure<bool>(Error.NotFound("Backup.NotFound", "Резервная копия не найдена."));

        var valid = await backup.ValidateBackupIntegrityAsync(record.FilePath, ct);
        var trackedRecord = await unitOfWork.BackupRecords.GetByIdAsync(record.Id, ct);
        if (trackedRecord is null)
            return Result.Failure<bool>(Error.NotFound("Backup.NotFound", "Резервная копия не найдена."));
        trackedRecord.MarkValidated(valid, trackedRecord.Note);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(valid);
    }
}

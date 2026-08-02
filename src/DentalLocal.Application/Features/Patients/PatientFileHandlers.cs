using System.Security.Cryptography;
using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Patients;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Patients;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetPatientFileByIdQuery(Guid FileId) : IQuery<PatientFileDto>;

public sealed class GetPatientFileByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetPatientFileByIdQuery, Result<PatientFileDto>>
{
    public async Task<Result<PatientFileDto>> Handle(GetPatientFileByIdQuery request, CancellationToken cancellationToken)
    {
        var file = await unitOfWork.PatientFiles.GetByIdAsync(request.FileId, cancellationToken);
        if (file is null)
            return Result.Failure<PatientFileDto>(Error.NotFound("PatientFile.NotFound", "Файл не найден."));

        return Result.Success(mapper.Map<PatientFileDto>(file));
    }
}

public sealed record GetPatientFilesQuery(Guid PatientId) : IQuery<IReadOnlyList<PatientFileDto>>;

public sealed class GetPatientFilesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetPatientFilesQuery, Result<IReadOnlyList<PatientFileDto>>>
{
    public async Task<Result<IReadOnlyList<PatientFileDto>>> Handle(
        GetPatientFilesQuery request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<IReadOnlyList<PatientFileDto>>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        var files = await unitOfWork.PatientFiles.GetByPatientIdAsync(request.PatientId, cancellationToken);
        return Result.Success(mapper.Map<IReadOnlyList<PatientFileDto>>(files));
    }
}

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record UploadPatientFileCommand(
    Guid PatientId,
    short FileType,
    string FileName,
    string ContentType,
    long SizeBytes,
    byte[] FileContent,
    string? Description,
    string? BeforeAfterTag = null) : ICommand<PatientFileDto>;

public sealed class UploadPatientFileCommandValidator : AbstractValidator<UploadPatientFileCommand>
{
    public UploadPatientFileCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SizeBytes).ExclusiveBetween(0, PatientFile.MaxFileSizeBytes)
            .WithMessage("Файл пустой или его размер превышает лимит 200 МБ.");
        RuleFor(x => x.FileContent).NotEmpty();
    }
}

public sealed class UploadPatientFileCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UploadPatientFileCommand, Result<PatientFileDto>>
{
    public async Task<Result<PatientFileDto>> Handle(UploadPatientFileCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<PatientFileDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        var fileId = Guid.NewGuid();
        var ext = Path.GetExtension(request.FileName);

        if (!PatientFile.AllowedExtensions.Contains(ext))
            return Result.Failure<PatientFileDto>(Error.Validation("PatientFile.Invalid", $"Недопустимый тип файла: «{ext}»."));

        // Сигнатура (magic bytes) проверяется ДО записи на диск — переименованный .exe в .png
        // не должен попасть в Data/Files даже временно.
        if (!PatientFile.HasValidSignature(ext, request.FileContent))
            return Result.Failure<PatientFileDto>(Error.Validation("PatientFile.InvalidSignature", "Содержимое файла не соответствует его расширению."));

        // Атомарность файл+БД (см. промпт 13 аудита): 1) пишем во временный файл в Temp/, а не сразу
        // в целевой путь, 2) считаем SHA-256, 3) коммитим запись в БД, 4) ТОЛЬКО ПОСЛЕ этого — атомарный
        // File.Move в Data/Files/{patientId}. Откат транзакции (например, из-за отмены запроса) больше
        // не оставляет файл на диске без парной записи в БД, а сбой на шаге 3 не оставляет запись
        // без файла — временный файл в обоих случаях просто удаляется.
        var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "Temp");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, $"upload_{fileId}{ext}");

        string sha256Hash;
        try
        {
            await File.WriteAllBytesAsync(tempPath, request.FileContent, cancellationToken);
            sha256Hash = Convert.ToHexString(SHA256.HashData(request.FileContent)).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            DeleteIfExists(tempPath);
            return Result.Failure<PatientFileDto>(Error.Failure("File.SaveFailed", $"Не удалось сохранить файл на диск: {ex.Message}"));
        }

        var relativePath = $"Data/Files/{request.PatientId}/{fileId}{ext}";

        PatientFile patientFile;
        try
        {
            patientFile = PatientFile.Create(
                request.PatientId,
                (PatientFileType)request.FileType,
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                relativePath,
                request.Description,
                sha256Hash: sha256Hash,
                beforeAfterType: request.BeforeAfterTag);
        }
        catch (ArgumentException ex)
        {
            DeleteIfExists(tempPath);
            return Result.Failure<PatientFileDto>(Error.Validation("PatientFile.Invalid", ex.Message));
        }

        try
        {
            await unitOfWork.PatientFiles.AddAsync(patientFile, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            DeleteIfExists(tempPath);
            throw;
        }

        // Запись в БД зафиксирована — теперь можно безопасно переместить файл в целевой путь.
        var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Files", request.PatientId.ToString());
        Directory.CreateDirectory(baseDir);
        var physicalPath = Path.Combine(baseDir, $"{fileId}{ext}");

        try
        {
            File.Move(tempPath, physicalPath, overwrite: true);
        }
        catch (Exception ex)
        {
            return Result.Failure<PatientFileDto>(Error.Failure("File.MoveFailed",
                $"Запись о файле сохранена, но не удалось переместить файл на постоянное хранение: {ex.Message}"));
        }

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("PATIENT_FILE_UPLOADED", "PatientFile", patientFile.Id, $"FileName: {patientFile.FileName}, PatientId: {request.PatientId}"),
            cancellationToken);

        return Result.Success(mapper.Map<PatientFileDto>(patientFile));
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Уборка временного файла не критична для результата операции — фоновая уборка "сирот"
            // в DailyMaintenanceBackgroundService подчистит его позже, если это не удалось сейчас.
        }
    }
}

// ---------------------------------------------------------------------------

public sealed record DeletePatientFileCommand(Guid Id) : ICommand;

public sealed class DeletePatientFileCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePatientFileCommand, Result>
{
    public async Task<Result> Handle(DeletePatientFileCommand request, CancellationToken cancellationToken)
    {
        var file = await unitOfWork.PatientFiles.GetByIdAsync(request.Id, cancellationToken);
        if (file is null)
            return Result.Failure(Error.NotFound("PatientFile.NotFound", "Файл не найден."));

        // Delete from database
        unitOfWork.PatientFiles.Remove(file);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Delete from disk
        var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), file.StoredPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physicalPath))
        {
            try
            {
                File.Delete(physicalPath);
            }
            catch
            {
                // Non-blocking file deletion failure
            }
        }

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("PATIENT_FILE_DELETED", "PatientFile", file.Id, $"FileName: {file.FileName}, PatientId: {file.PatientId}"),
            cancellationToken);

        return Result.Success();
    }
}

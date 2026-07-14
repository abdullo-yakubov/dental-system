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
    string? Description) : ICommand<PatientFileDto>;

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
        
        // Define directory: Data/Files/{patientId}
        var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Files", request.PatientId.ToString());
        Directory.CreateDirectory(baseDir);

        var physicalPath = Path.Combine(baseDir, $"{fileId}{ext}");
        var relativePath = $"Data/Files/{request.PatientId}/{fileId}{ext}";

        try
        {
            await File.WriteAllBytesAsync(physicalPath, request.FileContent, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result.Failure<PatientFileDto>(Error.Failure("File.SaveFailed", $"Не удалось сохранить файл на диск: {ex.Message}"));
        }

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
                request.Description);
        }
        catch (ArgumentException ex)
        {
            // Clean up file if validation failed
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
            return Result.Failure<PatientFileDto>(Error.Validation("PatientFile.Invalid", ex.Message));
        }

        // Add to context
        await unitOfWork.PatientFiles.AddAsync(patientFile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("PATIENT_FILE_UPLOADED", "PatientFile", patientFile.Id, $"FileName: {patientFile.FileName}, PatientId: {request.PatientId}"),
            cancellationToken);

        return Result.Success(mapper.Map<PatientFileDto>(patientFile));
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

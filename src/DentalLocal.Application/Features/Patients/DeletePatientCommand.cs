using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using MediatR;

namespace DentalLocal.Application.Features.Patients;

/// <summary>Безвозвратное удаление пациента (в отличие от <see cref="ArchivePatientCommand"/>, которое обратимо).</summary>
public sealed record DeletePatientCommand(Guid PatientId) : ICommand;

public sealed class DeletePatientCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeletePatientCommand, Result>
{
    public async Task<Result> Handle(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        var fullName = patient.FullName;

        await unitOfWork.Patients.DeleteWithRelatedDataAsync(request.PatientId, cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("PATIENT_DELETED", "Patient", request.PatientId, $"FullName: {fullName}"),
            cancellationToken);

        // Файлы пациента на диске не участвуют в транзакции БД — подчищаются отдельно и не блокируют результат,
        // как и при удалении одиночного файла (см. DeletePatientFileCommandHandler).
        var filesDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Files", request.PatientId.ToString());
        try
        {
            if (Directory.Exists(filesDir))
                Directory.Delete(filesDir, recursive: true);
        }
        catch
        {
            // Не критично для результата операции.
        }

        return Result.Success();
    }
}

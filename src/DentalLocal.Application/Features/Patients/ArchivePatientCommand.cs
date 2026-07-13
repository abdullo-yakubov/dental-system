using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using MediatR;

namespace DentalLocal.Application.Features.Patients;

public sealed record ArchivePatientCommand(Guid PatientId) : ICommand;

public sealed class ArchivePatientCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ArchivePatientCommand, Result>
{
    public async Task<Result> Handle(ArchivePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        patient.Archive();
        await unitOfWork.AuditLogs.AddAsync(AuditLog.Create("PATIENT_ARCHIVED", "Patient", patient.Id, null), cancellationToken);

        return Result.Success();
    }
}

public sealed record UnarchivePatientCommand(Guid PatientId) : ICommand;

public sealed class UnarchivePatientCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UnarchivePatientCommand, Result>
{
    public async Task<Result> Handle(UnarchivePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        patient.Unarchive();
        await unitOfWork.AuditLogs.AddAsync(AuditLog.Create("PATIENT_UNARCHIVED", "Patient", patient.Id, null), cancellationToken);

        return Result.Success();
    }
}

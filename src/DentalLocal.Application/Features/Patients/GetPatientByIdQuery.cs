using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Patients;
using MediatR;

namespace DentalLocal.Application.Features.Patients;

public sealed record GetPatientByIdQuery(Guid PatientId) : IQuery<PatientDto>;

public sealed class GetPatientByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetPatientByIdQuery, Result<PatientDto>>
{
    public async Task<Result<PatientDto>> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdWithDetailsAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<PatientDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        return mapper.Map<PatientDto>(patient);
    }
}

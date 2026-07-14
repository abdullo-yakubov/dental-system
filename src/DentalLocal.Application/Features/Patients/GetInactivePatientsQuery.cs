using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Patients;
using MediatR;

namespace DentalLocal.Application.Features.Patients;

/// <summary>Отчёт "пациенты без визита более N месяцев" (п.17.13 ТЗ).</summary>
public sealed record GetInactivePatientsQuery(int MonthsThreshold = 6) : IQuery<IReadOnlyList<PatientListItemDto>>;

public sealed class GetInactivePatientsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetInactivePatientsQuery, Result<IReadOnlyList<PatientListItemDto>>>
{
    public async Task<Result<IReadOnlyList<PatientListItemDto>>> Handle(
        GetInactivePatientsQuery request, CancellationToken cancellationToken)
    {
        var threshold = Math.Max(1, request.MonthsThreshold);
        var patients = await unitOfWork.Patients.GetInactiveAsync(threshold, cancellationToken);
        return Result.Success(mapper.Map<IReadOnlyList<PatientListItemDto>>(patients));
    }
}

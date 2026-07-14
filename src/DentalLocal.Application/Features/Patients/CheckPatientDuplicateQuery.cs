using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Patients;
using MediatR;

namespace DentalLocal.Application.Features.Patients;

/// <summary>Защита от задвоения карт (п.17.13 ТЗ): поиск похожих пациентов до создания новой карты.</summary>
public sealed record CheckPatientDuplicateQuery(string LastName, string FirstName, DateOnly BirthDate)
    : IQuery<IReadOnlyList<PossibleDuplicatePatientDto>>;

public sealed class CheckPatientDuplicateQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CheckPatientDuplicateQuery, Result<IReadOnlyList<PossibleDuplicatePatientDto>>>
{
    public async Task<Result<IReadOnlyList<PossibleDuplicatePatientDto>>> Handle(
        CheckPatientDuplicateQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.FirstName))
            return Result.Success<IReadOnlyList<PossibleDuplicatePatientDto>>([]);

        var duplicates = await unitOfWork.Patients.FindPossibleDuplicatesAsync(
            request.LastName, request.FirstName, request.BirthDate, cancellationToken);

        var dtos = duplicates
            .Select(p => new PossibleDuplicatePatientDto(p.Id, p.FullName, p.BirthDate, p.Phone))
            .ToList();

        return Result.Success<IReadOnlyList<PossibleDuplicatePatientDto>>(dtos);
    }
}

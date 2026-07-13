using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Patients;
using MediatR;

namespace DentalLocal.Application.Features.Patients;

public sealed record SearchPatientsQuery(string? Search, bool IncludeArchived, int? Page, int? PageSize)
    : IQuery<PagedResult<PatientListItemDto>>;

public sealed class SearchPatientsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<SearchPatientsQuery, Result<PagedResult<PatientListItemDto>>>
{
    public async Task<Result<PagedResult<PatientListItemDto>>> Handle(SearchPatientsQuery request, CancellationToken cancellationToken)
    {
        var page = PaginationDefaults.NormalizePage(request.Page);
        var pageSize = PaginationDefaults.NormalizePageSize(request.PageSize);

        var (items, totalCount) = await unitOfWork.Patients.SearchAsync(
            request.Search, request.IncludeArchived, page, pageSize, cancellationToken);

        var dtoItems = mapper.Map<IReadOnlyList<PatientListItemDto>>(items);

        return PagedResult<PatientListItemDto>.Create(dtoItems, totalCount, page, pageSize);
    }
}

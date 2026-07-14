using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Visits;
using MediatR;

namespace DentalLocal.Application.Features.Visits;

public sealed record GetVisitByIdQuery(Guid Id) : IQuery<VisitDto>;

public sealed class GetVisitByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetVisitByIdQuery, Result<VisitDto>>
{
    public async Task<Result<VisitDto>> Handle(GetVisitByIdQuery request, CancellationToken cancellationToken)
    {
        var visit = await unitOfWork.Visits.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (visit is null)
            return Result.Failure<VisitDto>(Error.NotFound("Visit.NotFound", "Визит не найден."));

        return Result.Success(mapper.Map<VisitDto>(visit));
    }
}

// ---------------------------------------------------------------------------

public sealed record GetPatientVisitsQuery(
    Guid PatientId,
    int Page = 1,
    int PageSize = 20) : IQuery<IReadOnlyList<VisitListItemDto>>;

public sealed class GetPatientVisitsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetPatientVisitsQuery, Result<IReadOnlyList<VisitListItemDto>>>
{
    public async Task<Result<IReadOnlyList<VisitListItemDto>>> Handle(
        GetPatientVisitsQuery request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<IReadOnlyList<VisitListItemDto>>(
                Error.NotFound("Patient.NotFound", "Пациент не найден."));

        var visits = await unitOfWork.Visits.GetByPatientIdAsync(request.PatientId, cancellationToken);
        var dtos = mapper.Map<IReadOnlyList<VisitListItemDto>>(visits);
        return Result.Success(dtos);
    }
}

// ---------------------------------------------------------------------------

public sealed record GetUpcomingVisitsQuery(int DaysAhead = 7, DateOnly? FromDate = null) : IQuery<IReadOnlyList<VisitListItemDto>>;

public sealed class GetUpcomingVisitsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetUpcomingVisitsQuery, Result<IReadOnlyList<VisitListItemDto>>>
{
    public async Task<Result<IReadOnlyList<VisitListItemDto>>> Handle(
        GetUpcomingVisitsQuery request, CancellationToken cancellationToken)
    {
        var baseDate = request.FromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var offsetDays = Math.Clamp(request.DaysAhead, -90, 90);
        var calculatedDate = baseDate.AddDays(offsetDays);

        var from = offsetDays < 0 ? calculatedDate : baseDate;
        var to = offsetDays < 0 ? baseDate : calculatedDate;

        var visits = await unitOfWork.Visits.GetUpcomingAsync(from, to, cancellationToken);
        return Result.Success(mapper.Map<IReadOnlyList<VisitListItemDto>>(visits));
    }
}

// ---------------------------------------------------------------------------

/// <summary>Полнотекстовый поиск по заметкам врача и диагнозу визитов (раздел 17.6 ТЗ).</summary>
public sealed record SearchVisitsByNotesQuery(string SearchText, int? Page, int? PageSize)
    : IQuery<PagedResult<VisitListItemDto>>;

public sealed class SearchVisitsByNotesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<SearchVisitsByNotesQuery, Result<PagedResult<VisitListItemDto>>>
{
    public async Task<Result<PagedResult<VisitListItemDto>>> Handle(
        SearchVisitsByNotesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchText))
            return PagedResult<VisitListItemDto>.Create([], 0, 1, PaginationDefaults.NormalizePageSize(request.PageSize));

        var page = PaginationDefaults.NormalizePage(request.Page);
        var pageSize = PaginationDefaults.NormalizePageSize(request.PageSize);

        var (items, totalCount) = await unitOfWork.Visits.SearchByNotesAsync(
            request.SearchText.Trim(), page, pageSize, cancellationToken);

        var dtoItems = mapper.Map<IReadOnlyList<VisitListItemDto>>(items);

        return PagedResult<VisitListItemDto>.Create(dtoItems, totalCount, page, pageSize);
    }
}

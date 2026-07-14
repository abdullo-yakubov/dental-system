using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Settings;
using MediatR;

namespace DentalLocal.Application.Features.Settings;

public sealed record GetAuditLogsQuery(int? Page, int? PageSize) : IQuery<PagedResult<AuditLogDto>>;

public sealed class GetAuditLogsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogDto>>>
{
    public async Task<Result<PagedResult<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var page = PaginationDefaults.NormalizePage(request.Page);
        var pageSize = PaginationDefaults.NormalizePageSize(request.PageSize);

        var (items, totalCount) = await unitOfWork.AuditLogs.SearchAsync(page, pageSize, cancellationToken);

        var dtoItems = items
            .Select(x => new AuditLogDto(x.Id, x.OccurredAt, x.Action, x.Entity, x.EntityId, x.DetailsJson))
            .ToList();

        return PagedResult<AuditLogDto>.Create(dtoItems, totalCount, page, pageSize);
    }
}

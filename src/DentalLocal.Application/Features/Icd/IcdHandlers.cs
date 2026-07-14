using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Icd;
using MediatR;

namespace DentalLocal.Application.Features.Icd;

public sealed record SearchIcd10CodesQuery(string? Search) : IQuery<IReadOnlyList<IcdCodeDto>>;

public sealed class SearchIcd10CodesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SearchIcd10CodesQuery, Result<IReadOnlyList<IcdCodeDto>>>
{
    public async Task<Result<IReadOnlyList<IcdCodeDto>>> Handle(SearchIcd10CodesQuery request, CancellationToken cancellationToken)
    {
        var codes = await unitOfWork.Icd10Codes.SearchAsync(request.Search, 30, cancellationToken);
        return Result.Success<IReadOnlyList<IcdCodeDto>>(
            codes.Select(c => new IcdCodeDto(c.Code, c.Description)).ToList());
    }
}

// ---------------------------------------------------------------------------

public sealed record SearchIcd11CodesQuery(string? Search) : IQuery<IReadOnlyList<IcdCodeDto>>;

public sealed class SearchIcd11CodesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SearchIcd11CodesQuery, Result<IReadOnlyList<IcdCodeDto>>>
{
    public async Task<Result<IReadOnlyList<IcdCodeDto>>> Handle(SearchIcd11CodesQuery request, CancellationToken cancellationToken)
    {
        var codes = await unitOfWork.Icd11Codes.SearchAsync(request.Search, 30, cancellationToken);
        return Result.Success<IReadOnlyList<IcdCodeDto>>(
            codes.Select(c => new IcdCodeDto(c.Code, c.Description)).ToList());
    }
}

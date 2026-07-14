using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Icd;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace DentalLocal.Application.Features.Icd;

public sealed record SearchIcd10CodesQuery(string? Search) : IQuery<IReadOnlyList<IcdCodeDto>>;

public sealed class SearchIcd10CodesQueryHandler(IUnitOfWork unitOfWork, IMemoryCache cache)
    : IRequestHandler<SearchIcd10CodesQuery, Result<IReadOnlyList<IcdCodeDto>>>
{
    public async Task<Result<IReadOnlyList<IcdCodeDto>>> Handle(SearchIcd10CodesQuery request, CancellationToken cancellationToken)
    {
        // Справочник МКБ-10 редко меняется — кэшируем полный список, поиск фильтруем в памяти (раздел 12 ТЗ).
        var all = await cache.GetOrCreateAsync(CacheKeys.Icd10Codes, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await unitOfWork.Icd10Codes.SearchAsync(null, int.MaxValue, cancellationToken);
        });

        var filtered = IcdSearch.Filter(all!, request.Search, 30, c => c.Code, c => c.Description);
        return Result.Success<IReadOnlyList<IcdCodeDto>>(
            filtered.Select(c => new IcdCodeDto(c.Code, c.Description)).ToList());
    }
}

// ---------------------------------------------------------------------------

public sealed record SearchIcd11CodesQuery(string? Search) : IQuery<IReadOnlyList<IcdCodeDto>>;

public sealed class SearchIcd11CodesQueryHandler(IUnitOfWork unitOfWork, IMemoryCache cache)
    : IRequestHandler<SearchIcd11CodesQuery, Result<IReadOnlyList<IcdCodeDto>>>
{
    public async Task<Result<IReadOnlyList<IcdCodeDto>>> Handle(SearchIcd11CodesQuery request, CancellationToken cancellationToken)
    {
        var all = await cache.GetOrCreateAsync(CacheKeys.Icd11Codes, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await unitOfWork.Icd11Codes.SearchAsync(null, int.MaxValue, cancellationToken);
        });

        var filtered = IcdSearch.Filter(all!, request.Search, 30, c => c.Code, c => c.Description);
        return Result.Success<IReadOnlyList<IcdCodeDto>>(
            filtered.Select(c => new IcdCodeDto(c.Code, c.Description)).ToList());
    }
}

internal static class IcdSearch
{
    public static IEnumerable<T> Filter<T>(
        IReadOnlyList<T> all, string? search, int limit, Func<T, string> code, Func<T, string> description)
    {
        var query = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = all.Where(c => Matches(code(c), trimmed) || Matches(description(c), trimmed));
        }

        return query.Take(limit);
    }

    private static bool Matches(string value, string search) =>
        value.Contains(search, StringComparison.OrdinalIgnoreCase);
}

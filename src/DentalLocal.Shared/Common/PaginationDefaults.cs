namespace DentalLocal.Shared.Common;

public static class PaginationDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 200;

    public static int NormalizePage(int? page) => page is > 0 ? page.Value : DefaultPage;

    public static int NormalizePageSize(int? pageSize) =>
        pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;
}

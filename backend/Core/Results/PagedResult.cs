namespace Core.Results;

public sealed record PagedResult<T>(
    int Page,
    int PageSize,
    int TotalCount,
    IEnumerable<T> Items);
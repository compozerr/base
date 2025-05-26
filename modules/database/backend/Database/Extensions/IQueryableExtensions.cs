namespace Database.Extensions;

public static class IQueryableExtensions
{
    public static IQueryable<T> AsPageable<T>(this IQueryable<T> query, int page, int pageSize)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");

        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}
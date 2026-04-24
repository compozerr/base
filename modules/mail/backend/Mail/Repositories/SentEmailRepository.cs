using Database.Repositories;
using Mail.Data;
using Mail.Models;
using Microsoft.EntityFrameworkCore;

namespace Mail.Repositories;

public class SentEmailRepository(MailDbContext context)
    : GenericRepository<SentEmail, SentEmailId, MailDbContext>(context), ISentEmailRepository
{
    public async Task<(IReadOnlyList<SentEmail> Items, DateTime? NextCursor)> ListAsync(
        DateTime? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = Query().AsNoTracking().OrderByDescending(e => e.SentAtUtc);

        var filtered = cursor is { } nonNullCursor
            ? query.Where(e => e.SentAtUtc < nonNullCursor)
            : (IQueryable<SentEmail>)query;

        var rows = await filtered.Take(limit + 1).ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var items = hasMore ? rows.Take(limit).ToList() : rows;
        var nextCursor = hasMore ? items[^1].SentAtUtc : (DateTime?)null;

        return (items, nextCursor);
    }
}

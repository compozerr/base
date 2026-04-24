using Mail.Data;
using Mail.Models;
using Microsoft.EntityFrameworkCore;

namespace Mail.Repositories;

public class SentEmailRepository(MailDbContext dbContext) : ISentEmailRepository
{
    public async Task AddAsync(SentEmail sentEmail, CancellationToken cancellationToken = default)
    {
        dbContext.SentEmails.Add(sentEmail);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<SentEmail?> GetByIdAsync(SentEmailId id, CancellationToken cancellationToken = default)
        => dbContext.SentEmails
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<SentEmail> Items, DateTime? NextCursor)> ListAsync(
        DateTime? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SentEmails.AsNoTracking()
                                        .OrderByDescending(e => e.SentAtUtc)
                                        .AsQueryable();

        if (cursor is { } nonNullCursor)
        {
            query = query.Where(e => e.SentAtUtc < nonNullCursor);
        }

        // Fetch one extra to detect whether a next page exists
        var rows = await query.Take(limit + 1).ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var items = hasMore ? rows.Take(limit).ToList() : rows;
        var nextCursor = hasMore ? items[^1].SentAtUtc : (DateTime?)null;

        return (items, nextCursor);
    }
}

using Database.Repositories;
using Mail.Data;
using Mail.Models;

namespace Mail.Repositories;

public interface ISentEmailRepository : IGenericRepository<SentEmail, SentEmailId, MailDbContext>
{
    Task<(IReadOnlyList<SentEmail> Items, DateTime? NextCursor)> ListAsync(
        DateTime? cursor,
        int limit,
        CancellationToken cancellationToken = default);
}

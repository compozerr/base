using Mail.Models;

namespace Mail.Repositories;

public interface ISentEmailRepository
{
    Task AddAsync(SentEmail sentEmail, CancellationToken cancellationToken = default);
    Task<SentEmail?> GetByIdAsync(SentEmailId id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SentEmail> Items, DateTime? NextCursor)> ListAsync(
        DateTime? cursor,
        int limit,
        CancellationToken cancellationToken = default);
}

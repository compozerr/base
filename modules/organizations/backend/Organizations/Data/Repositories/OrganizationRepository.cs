using Auth.Abstractions;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Organizations.Abstractions;
using Organizations.Models;

namespace Organizations.Data.Repositories;

public interface IOrganizationRepository : IGenericRepository<Organization, OrganizationId, OrganizationsDbContext>
{
    Task<Organization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Organization?> GetPersonalOrgForUserAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<List<Organization>> GetOrganizationsForUserAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<Organization?> GetByIdWithMembersAsync(OrganizationId id, CancellationToken cancellationToken = default);
}

public sealed class OrganizationRepository(
    OrganizationsDbContext context) : GenericRepository<Organization, OrganizationId, OrganizationsDbContext>(context), IOrganizationRepository
{
    public Task<Organization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => Query().FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

    public Task<Organization?> GetPersonalOrgForUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => Query().FirstOrDefaultAsync(x => x.OwnerUserId == userId && x.IsPersonal, cancellationToken);

    public Task<List<Organization>> GetOrganizationsForUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => context.OrganizationMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Organization)
            .Select(m => m.Organization!)
            .Where(o => o.DeletedAtUtc == null)
            .ToListAsync(cancellationToken);

    public Task<Organization?> GetByIdWithMembersAsync(OrganizationId id, CancellationToken cancellationToken = default)
        => Query()
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}

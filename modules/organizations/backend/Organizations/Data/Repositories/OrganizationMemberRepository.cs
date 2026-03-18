using Auth.Abstractions;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Organizations.Abstractions;
using Organizations.Models;

namespace Organizations.Data.Repositories;

public interface IOrganizationMemberRepository : IGenericRepository<OrganizationMember, OrganizationMemberId, OrganizationsDbContext>
{
    Task<OrganizationMember?> GetMemberAsync(OrganizationId orgId, UserId userId, CancellationToken cancellationToken = default);
    Task<List<OrganizationMember>> GetMembersForOrganizationAsync(OrganizationId orgId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(OrganizationId orgId, UserId userId, CancellationToken cancellationToken = default);
    Task<bool> IsAdminAsync(OrganizationId orgId, UserId userId, CancellationToken cancellationToken = default);
}

public sealed class OrganizationMemberRepository(
    OrganizationsDbContext context) : GenericRepository<OrganizationMember, OrganizationMemberId, OrganizationsDbContext>(context), IOrganizationMemberRepository
{
    public Task<OrganizationMember?> GetMemberAsync(OrganizationId orgId, UserId userId, CancellationToken cancellationToken = default)
        => Query().FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.UserId == userId, cancellationToken);

    public Task<List<OrganizationMember>> GetMembersForOrganizationAsync(OrganizationId orgId, CancellationToken cancellationToken = default)
        => Query().Where(x => x.OrganizationId == orgId).ToListAsync(cancellationToken);

    public Task<bool> IsMemberAsync(OrganizationId orgId, UserId userId, CancellationToken cancellationToken = default)
        => Query().AnyAsync(x => x.OrganizationId == orgId && x.UserId == userId, cancellationToken);

    public Task<bool> IsAdminAsync(OrganizationId orgId, UserId userId, CancellationToken cancellationToken = default)
        => Query().AnyAsync(x => x.OrganizationId == orgId && x.UserId == userId && x.Role == OrganizationRole.Admin, cancellationToken);
}

using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Organizations.Abstractions;
using Organizations.Models;

namespace Organizations.Data.Repositories;

public interface IOrganizationInvitationRepository : IGenericRepository<OrganizationInvitation, OrganizationInvitationId, OrganizationsDbContext>
{
    Task<OrganizationInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<OrganizationInvitation>> GetPendingForOrganizationAsync(OrganizationId orgId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingInvitationAsync(OrganizationId orgId, string email, CancellationToken cancellationToken = default);
}

public sealed class OrganizationInvitationRepository(
    OrganizationsDbContext context) : GenericRepository<OrganizationInvitation, OrganizationInvitationId, OrganizationsDbContext>(context), IOrganizationInvitationRepository
{
    public Task<OrganizationInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => Query()
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

    public Task<List<OrganizationInvitation>> GetPendingForOrganizationAsync(OrganizationId orgId, CancellationToken cancellationToken = default)
        => Query()
            .Where(x => x.OrganizationId == orgId && x.Status == InvitationStatus.Pending)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> HasPendingInvitationAsync(OrganizationId orgId, string email, CancellationToken cancellationToken = default)
        => Query().AnyAsync(x => x.OrganizationId == orgId && x.Email == email && x.Status == InvitationStatus.Pending, cancellationToken);
}

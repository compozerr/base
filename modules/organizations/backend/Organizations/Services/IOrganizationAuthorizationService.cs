using Auth.Abstractions;
using Organizations.Abstractions;

namespace Organizations.Services;

public interface IOrganizationAuthorizationService
{
    Task<bool> IsMemberAsync(OrganizationId orgId, UserId userId, CancellationToken cancellationToken = default);
    Task<bool> IsAdminAsync(OrganizationId orgId, UserId userId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessProjectAsync(OrganizationId? projectOrgId, UserId userId, CancellationToken cancellationToken = default);
}

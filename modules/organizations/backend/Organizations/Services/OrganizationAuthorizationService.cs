using Auth.Abstractions;
using Organizations.Abstractions;
using Organizations.Data.Repositories;

namespace Organizations.Services;

public sealed class OrganizationAuthorizationService(
    IOrganizationMemberRepository memberRepository) : IOrganizationAuthorizationService
{
    public Task<bool> IsMemberAsync(OrganizationId orgId, UserId userId, CancellationToken cancellationToken = default)
        => memberRepository.IsMemberAsync(orgId, userId, cancellationToken);

    public Task<bool> IsAdminAsync(OrganizationId orgId, UserId userId, CancellationToken cancellationToken = default)
        => memberRepository.IsAdminAsync(orgId, userId, cancellationToken);

    public async Task<bool> CanAccessProjectAsync(OrganizationId? projectOrgId, UserId userId, CancellationToken cancellationToken = default)
    {
        if (projectOrgId is null)
            return false;

        return await memberRepository.IsMemberAsync(projectOrgId, userId, cancellationToken);
    }
}

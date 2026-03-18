using Auth.Abstractions;
using Auth.Services;
using Microsoft.AspNetCore.Http;
using Organizations.Abstractions;
using Organizations.Data.Repositories;

namespace Organizations.Services;

public sealed class OrganizationContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    ICurrentUserAccessor currentUserAccessor,
    IOrganizationRepository organizationRepository) : IOrganizationContextAccessor
{
    private const string OrganizationIdHeader = "X-Organization-Id";

    public async Task<OrganizationId> GetCurrentOrganizationIdAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;

        // Try to get from header
        if (httpContext?.Request.Headers.TryGetValue(OrganizationIdHeader, out var headerValue) == true
            && !string.IsNullOrEmpty(headerValue)
            && OrganizationId.TryParse(headerValue!, out var orgId))
        {
            return orgId;
        }

        // Default to personal org
        var userId = currentUserAccessor.CurrentUserId;
        if (userId is null)
            throw new InvalidOperationException("User is not authenticated.");

        var personalOrg = await organizationRepository.GetPersonalOrgForUserAsync(userId);
        if (personalOrg is null)
            throw new InvalidOperationException("User does not have a personal organization.");

        return personalOrg.Id;
    }
}

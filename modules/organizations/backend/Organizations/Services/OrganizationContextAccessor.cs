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
    private OrganizationId? _cached;

    public async Task<OrganizationId> GetCurrentOrganizationIdAsync()
    {
        if (_cached is not null) return _cached;
        var httpContext = httpContextAccessor.HttpContext;

        // Try to get from header
        if (httpContext?.Request.Headers.TryGetValue(OrganizationIdHeader, out var headerValue) == true
            && !string.IsNullOrEmpty(headerValue)
            && OrganizationId.TryParse(headerValue!, out var orgId))
        {
            _cached = orgId;
            return orgId;
        }

        // Default to personal org
        var userId = currentUserAccessor.CurrentUserId;
        if (userId is null)
            throw new InvalidOperationException("User is not authenticated.");

        var personalOrg = await organizationRepository.GetPersonalOrgForUserAsync(userId);
        if (personalOrg is null)
            throw new InvalidOperationException("User does not have a personal organization.");

        _cached = personalOrg.Id;
        return personalOrg.Id;
    }

    public async Task<OrganizationId?> GetCurrentOrganizationIdOrDefaultAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext?.Request.Headers.TryGetValue(OrganizationIdHeader, out var headerValue) == true
            && !string.IsNullOrEmpty(headerValue)
            && OrganizationId.TryParse(headerValue!, out var orgId))
        {
            return orgId;
        }

        var userId = currentUserAccessor.CurrentUserId;
        if (userId is null) return null;

        var personalOrg = await organizationRepository.GetPersonalOrgForUserAsync(userId);
        return personalOrg?.Id;
    }
}

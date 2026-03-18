using Organizations.Abstractions;

namespace Organizations.Services;

public interface IOrganizationContextAccessor
{
    /// <summary>
    /// Gets the current organization ID from the request context.
    /// Resolves from X-Organization-Id header, or defaults to the user's personal organization.
    /// </summary>
    Task<OrganizationId> GetCurrentOrganizationIdAsync();
}

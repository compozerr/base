using Auth.Abstractions;
using Organizations.Abstractions;

namespace Stripe.Services;

public interface ICurrentStripeCustomerIdAccessor
{
    /// <summary>
    /// Gets the stripe customer ID associated with the current request.
    /// Falls back to user-based lookup if no organization context is available.
    /// </summary>
    /// <returns>The customer ID as a string.</returns>
    Task<string> GetOrCreateStripeCustomerId();
    Task<string> GetOrCreateStripeCustomerId(string internalId);

    /// <summary>
    /// Gets or creates a Stripe customer for an organization.
    /// </summary>
    Task<string> GetOrCreateStripeCustomerIdForOrganization(OrganizationId orgId);
}
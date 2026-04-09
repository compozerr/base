using Organizations.Abstractions;

namespace Stripe.Services;

public interface ICurrentStripeCustomerIdAccessor
{
    /// <summary>
    /// Gets the Stripe customer ID for the current organization (from X-Organization-Id header).
    /// Creates a new Stripe customer for the org if one doesn't exist.
    /// </summary>
    Task<string> GetOrCreateStripeCustomerId();

    /// <summary>
    /// Gets or creates a Stripe customer for a specific organization.
    /// Use this in background/webhook handlers where there is no HTTP context.
    /// </summary>
    Task<string> GetOrCreateStripeCustomerIdForOrganization(OrganizationId orgId);
}

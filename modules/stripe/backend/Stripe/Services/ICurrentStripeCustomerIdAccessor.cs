using Auth.Abstractions;

namespace Stripe.Services;

public interface ICurrentStripeCustomerIdAccessor
{
    /// <summary>
    /// Gets the stripe customer ID associated with the current request.
    /// </summary>
    /// <returns>The customer ID as a string.</returns>
    Task<string> GetOrCreateStripeCustomerId();
    Task<string> GetOrCreateStripeCustomerId(string userId);
}
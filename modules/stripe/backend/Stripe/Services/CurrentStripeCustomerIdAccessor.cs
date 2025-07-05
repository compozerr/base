
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Stripe.Data.Repositories;
using Stripe.Options;

namespace Stripe.Services;

public sealed class CurrentStripeCustomerIdAccessor(
    IHttpContextAccessor httpContextAccessor,
    IStripeCustomerRepository stripeCustomerRepository,
    IOptions<StripeOptions> options) : ICurrentStripeCustomerIdAccessor
{
    private readonly StripeClient stripeClient = new(
        options.Value.ApiKey);

    public async Task<string> GetOrCreateStripeCustomerId()
    {
        var userId = GetUserIdFromContext();

        var existingCustomerId = await stripeCustomerRepository.GetStripeCustomerIdByInternalId(userId);
        if (existingCustomerId != null)
        {
            return await EnsureCustomerExistsAsync(existingCustomerId, userId);
        }

        var newCustomerId = await CreateCustomerAsync(userId);
        return newCustomerId;
    }

    private Task SetStripeCustomerIdAsync(string customerId)
    {
        var userId = GetUserIdFromContext();

        return stripeCustomerRepository.SetStripeCustomerIdAsync(userId, customerId);
    }

    private string GetUserIdFromContext()
    {
        return httpContextAccessor.HttpContext
                                  ?.User
                                  ?.FindFirst(ClaimTypes.NameIdentifier)
                                  ?.Value ?? throw new InvalidOperationException("User is not authenticated.");
    }

    /// <summary>
    /// Ensures that a customer exists in Stripe with the given ID
    /// </summary>
    /// <param name="stripeCustomerId">The user ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The Stripe customer ID (may be different from userId if created)</returns>
    private async Task<string> EnsureCustomerExistsAsync(string stripeCustomerId, string internalId, CancellationToken cancellationToken = default)
    {
        var customerService = new CustomerService(stripeClient);

        try
        {
            // Try to retrieve the customer
            await customerService.GetAsync(stripeCustomerId, cancellationToken: cancellationToken);
            return stripeCustomerId; // Customer exists, return the same ID
        }
        catch (StripeException ex) when (ex.ToString().Contains("no such customer", StringComparison.InvariantCultureIgnoreCase))
        {
            return await CreateCustomerAsync(internalId, cancellationToken);
        }
    }

    private async Task<string> CreateCustomerAsync(string internalId, CancellationToken cancellationToken = default)
    {
        var customerService = new CustomerService(stripeClient);
        var options = new CustomerCreateOptions
        {
            Description = $"Customer for user {internalId}",
            Name= $"User {internalId}",
            Metadata = new Dictionary<string, string>
            {
                { "InternalId", internalId }
            }
        };

        var customer = await customerService.CreateAsync(options, cancellationToken: cancellationToken);

        var customerId = customer.Id;

        await SetStripeCustomerIdAsync(customerId);

        return customerId;
    }
}


using System.Collections.Concurrent;
using System.Security.Claims;
using Auth.Abstractions;
using Auth.Models;
using Auth.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Stripe.Data.Repositories;
using Stripe.Options;

namespace Stripe.Services;

public sealed class CurrentStripeCustomerIdAccessor(
    IHttpContextAccessor httpContextAccessor,
    IStripeCustomerRepository stripeCustomerRepository,
    IUserRepository userRepository,
    IOptions<StripeOptions> options) : ICurrentStripeCustomerIdAccessor
{
    private readonly StripeClient stripeClient = new(
        options.Value.ApiKey);

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> UserLocks = new();

    private static SemaphoreSlim GetUserLock(string userId)
    {
        return UserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
    }


    public Task<string> GetOrCreateStripeCustomerId()
    {
        var userId = GetUserIdFromContext();
        return GetOrCreateStripeCustomerId(userId);
    }

    public async Task<string> GetOrCreateStripeCustomerId(string userId)
    {
        var userLock = GetUserLock(userId);

        await userLock.WaitAsync();
        try
        {
            var existingCustomerId = await stripeCustomerRepository.GetStripeCustomerIdByInternalIdAsync(userId);
            if (existingCustomerId != null)
            {
                return await EnsureCustomerExistsAsync(existingCustomerId, userId);
            }

            var newCustomerId = await CreateCustomerAsync(userId);
            return newCustomerId;
        }
        finally
        {
            userLock.Release();
        }
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
    /// Ensures that a customer exists in Stripe with the given ID and has an email
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
            var customer = await customerService.GetAsync(stripeCustomerId, cancellationToken: cancellationToken);
            if (customer.Deleted ?? false)
            {
                return await CreateCustomerAsync(internalId, cancellationToken);
            }

            // Ensure customer has an email (required for send_invoice collection method)
            if (string.IsNullOrEmpty(customer.Email))
            {
                await EnsureCustomerHasEmailAsync(stripeCustomerId, internalId, cancellationToken);
            }

            return stripeCustomerId; // Customer exists, return the same ID
        }
        catch (StripeException ex) when (ex.Message.Contains("no such customer", StringComparison.InvariantCultureIgnoreCase))
        {
            return await CreateCustomerAsync(internalId, cancellationToken);
        }
    }

    private async Task EnsureCustomerHasEmailAsync(string stripeCustomerId, string internalId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(internalId, cancellationToken);
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            return; // Cannot update without user email
        }

        var customerService = new CustomerService(stripeClient);
        var updateOptions = new CustomerUpdateOptions
        {
            Email = user.Email,
            Name = user.Name
        };

        await customerService.UpdateAsync(stripeCustomerId, updateOptions, cancellationToken: cancellationToken);
    }

    private async Task<string> CreateCustomerAsync(string internalId, CancellationToken cancellationToken = default)
    {
        // Get user details to include email
        var user = await GetUserAsync(internalId, cancellationToken);

        var customerService = new CustomerService(stripeClient);
        var options = new CustomerCreateOptions
        {
            Description = $"Customer for user {internalId}",
            Name = user?.Name ?? $"User {internalId}",
            Email = user?.Email, // Include email for send_invoice collection method
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

    private async Task<User?> GetUserAsync(string internalId, CancellationToken cancellationToken = default)
    {
        if (!UserId.TryParse(internalId, out var userId))
        {
            return null;
        }

        return await userRepository.GetByIdAsync(userId, cancellationToken);
    }
}

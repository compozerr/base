
using System.Collections.Concurrent;
using Auth.Abstractions;
using Auth.Models;
using Auth.Repositories;
using Microsoft.Extensions.Options;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Models;
using Organizations.Services;
using Stripe.Data.Repositories;
using Stripe.Options;

namespace Stripe.Services;

public sealed class CurrentStripeCustomerIdAccessor(
    IStripeCustomerRepository stripeCustomerRepository,
    IUserRepository userRepository,
    IOrganizationContextAccessor organizationContextAccessor,
    IOrganizationRepository organizationRepository,
    IOptions<StripeOptions> options) : ICurrentStripeCustomerIdAccessor
{
    private readonly StripeClient stripeClient = new(
        options.Value.ApiKey);

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> OrgLocks = new();

    private static SemaphoreSlim GetOrgLock(string orgId)
    {
        return OrgLocks.GetOrAdd(orgId, _ => new SemaphoreSlim(1, 1));
    }

    public async Task<string> GetOrCreateStripeCustomerId()
    {
        var orgId = await organizationContextAccessor.GetCurrentOrganizationIdAsync();
        return await GetOrCreateStripeCustomerIdForOrganization(orgId);
    }

    public async Task<string> GetOrCreateStripeCustomerIdForOrganization(OrganizationId orgId)
    {
        var orgIdString = orgId.Value.ToString();
        var orgLock = GetOrgLock(orgIdString);

        await orgLock.WaitAsync();
        try
        {
            var existingCustomerId = await stripeCustomerRepository.GetStripeCustomerIdByInternalIdAsync(orgIdString);
            if (existingCustomerId != null)
            {
                return await EnsureCustomerExistsAsync(existingCustomerId, orgId);
            }

            // Create new Stripe customer for the organization
            var org = await organizationRepository.GetByIdAsync(orgId);
            var ownerUser = org is not null ? await GetOwnerUserAsync(org.OwnerUserId.Value.ToString()) : null;

            var customerService = new CustomerService(stripeClient);
            var createOptions = BuildCustomerCreateOptions(org, orgIdString, ownerUser);
            var customer = await customerService.CreateAsync(createOptions);
            await stripeCustomerRepository.SetStripeCustomerIdAsync(orgIdString, customer.Id);
            return customer.Id;
        }
        finally
        {
            orgLock.Release();
        }
    }

    /// <summary>
    /// Ensures that a customer exists in Stripe with the given ID and has an email.
    /// </summary>
    private async Task<string> EnsureCustomerExistsAsync(string stripeCustomerId, OrganizationId orgId, CancellationToken cancellationToken = default)
    {
        var customerService = new CustomerService(stripeClient);

        try
        {
            // Try to retrieve the customer
            var customer = await customerService.GetAsync(stripeCustomerId, cancellationToken: cancellationToken);
            if (customer.Deleted ?? false)
            {
                return await RecreateCustomerAsync(orgId, cancellationToken);
            }

            // Ensure customer has an email (required for send_invoice collection method)
            if (string.IsNullOrEmpty(customer.Email))
            {
                await EnsureCustomerHasEmailAsync(stripeCustomerId, orgId, cancellationToken);
            }

            return stripeCustomerId; // Customer exists, return the same ID
        }
        catch (StripeException ex) when (ex.Message.Contains("no such customer", StringComparison.InvariantCultureIgnoreCase))
        {
            return await RecreateCustomerAsync(orgId, cancellationToken);
        }
    }

    private async Task EnsureCustomerHasEmailAsync(string stripeCustomerId, OrganizationId orgId, CancellationToken cancellationToken = default)
    {
        var org = await organizationRepository.GetByIdAsync(orgId, cancellationToken);
        if (org is null) return;

        var owner = await userRepository.GetByIdAsync(org.OwnerUserId, cancellationToken);
        if (owner is null || string.IsNullOrEmpty(owner.Email))
            return;

        var customerService = new CustomerService(stripeClient);
        var updateOptions = new CustomerUpdateOptions
        {
            Email = owner.Email,
            Name = org.Name
        };

        await customerService.UpdateAsync(stripeCustomerId, updateOptions, cancellationToken: cancellationToken);
    }

    private async Task<string> RecreateCustomerAsync(OrganizationId orgId, CancellationToken cancellationToken = default)
    {
        var orgIdString = orgId.Value.ToString();
        var org = await organizationRepository.GetByIdAsync(orgId, cancellationToken);
        var ownerUser = org is not null ? await GetOwnerUserAsync(org.OwnerUserId.Value.ToString(), cancellationToken) : null;

        var customerService = new CustomerService(stripeClient);
        var createOptions = BuildCustomerCreateOptions(org, orgIdString, ownerUser);
        var customer = await customerService.CreateAsync(createOptions, cancellationToken: cancellationToken);
        await stripeCustomerRepository.SetStripeCustomerIdAsync(orgIdString, customer.Id);
        return customer.Id;
    }

    private static CustomerCreateOptions BuildCustomerCreateOptions(Organization? org, string orgIdString, User? ownerUser)
    {
        return new CustomerCreateOptions
        {
            Description = $"Organization: {org?.Name ?? orgIdString}",
            Name = org?.Name ?? $"Organization {orgIdString}",
            Email = ownerUser?.Email,
            Metadata = new Dictionary<string, string>
            {
                { "InternalId", orgIdString },
                { "Type", "organization" }
            }
        };
    }

    private async Task<User?> GetOwnerUserAsync(string userIdString, CancellationToken cancellationToken = default)
    {
        if (!UserId.TryParse(userIdString, out var userId))
        {
            return null;
        }

        return await userRepository.GetByIdAsync(userId, cancellationToken);
    }
}

using Auth.Models;
using Auth.Repositories;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Models;
using Stripe.Data.Repositories;

namespace Stripe.Services;

public interface IStripeOrgResolver
{
    Task<StripeOrgOwner?> ResolveOrgAndOwnerByStripeCustomerIdAsync(
        string stripeCustomerId, CancellationToken cancellationToken = default);
}

public record StripeOrgOwner(OrganizationId OrgId, Organization Organization, User Owner);

public sealed class StripeOrgResolverService(
    IStripeCustomerRepository stripeCustomerRepository,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository) : IStripeOrgResolver
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.ForContext<StripeOrgResolverService>();

    public async Task<StripeOrgOwner?> ResolveOrgAndOwnerByStripeCustomerIdAsync(
        string stripeCustomerId, CancellationToken cancellationToken = default)
    {
        var internalId = await stripeCustomerRepository.GetInternalIdByStripeCustomerIdAsync(
            stripeCustomerId, cancellationToken);

        if (internalId is null || !OrganizationId.TryParse(internalId, out var orgId))
        {
            Logger.Error("Could not resolve organization for Stripe customer {CustomerId}: InternalId={InternalId}",
                stripeCustomerId, internalId);
            return null;
        }

        var org = await organizationRepository.GetByIdAsync(orgId, cancellationToken);
        if (org is null)
        {
            Logger.Error("Organization {OrgId} not found for Stripe customer {CustomerId}",
                orgId, stripeCustomerId);
            return null;
        }

        var owner = await userRepository.GetByIdAsync(org.OwnerUserId, cancellationToken);
        if (owner is null)
        {
            Logger.Error("Owner user {UserId} not found for organization {OrgId}",
                org.OwnerUserId, orgId);
            return null;
        }

        return new StripeOrgOwner(orgId, org, owner);
    }
}

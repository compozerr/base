using Microsoft.Extensions.Options;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Stripe.Data.Repositories;
using Stripe.Options;

namespace Stripe.Services;

public sealed class StripeCustomerMetadataSyncService(
    IStripeCustomerRepository stripeCustomerRepository,
    IOrganizationRepository organizationRepository,
    IOptions<StripeOptions> options)
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.ForContext<StripeCustomerMetadataSyncService>();

    public async Task SyncAllCustomerMetadataAsync(CancellationToken cancellationToken = default)
    {
        var stripeClient = new StripeClient(options.Value.ApiKey);
        var customerService = new CustomerService(stripeClient);

        var allCustomers = await stripeCustomerRepository.GetAllAsync(cancellationToken);

        foreach (var customer in allCustomers)
        {
            try
            {
                var stripeCustomer = await customerService.GetAsync(
                    customer.StripeCustomerId, cancellationToken: cancellationToken);

                // Skip if already synced
                if (stripeCustomer.Metadata.TryGetValue("Type", out var type) && type == "organization")
                    continue;

                // Look up org name for the customer description
                string orgName = customer.InternalId;
                if (OrganizationId.TryParse(customer.InternalId, out var orgId))
                {
                    var org = await organizationRepository.GetByIdAsync(orgId, cancellationToken);
                    if (org is not null)
                        orgName = org.Name;
                }

                await customerService.UpdateAsync(customer.StripeCustomerId, new CustomerUpdateOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "InternalId", customer.InternalId },
                        { "Type", "organization" }
                    },
                    Description = $"Organization: {orgName}",
                    Name = orgName,
                }, cancellationToken: cancellationToken);

                Logger.Information("Synced Stripe metadata for customer {CustomerId} (org: {OrgId})",
                    customer.StripeCustomerId, customer.InternalId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to sync metadata for Stripe customer {CustomerId}",
                    customer.StripeCustomerId);
            }
        }
    }
}

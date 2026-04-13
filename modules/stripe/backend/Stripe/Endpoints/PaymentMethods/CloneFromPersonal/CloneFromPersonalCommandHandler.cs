using Auth.Services;
using Core.MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Organizations.Data.Repositories;
using Organizations.Services;
using Serilog;
using Stripe.Data.Repositories;
using Stripe.Options;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.CloneFromPersonal;

public sealed class CloneFromPersonalCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IOrganizationContextAccessor organizationContextAccessor,
    IOrganizationAuthorizationService organizationAuthorizationService,
    IOrganizationRepository organizationRepository,
    IStripeCustomerRepository stripeCustomerRepository,
    ICurrentStripeCustomerIdAccessor currentStripeCustomerIdAccessor,
    IOptions<StripeOptions> options,
    IMemoryCache memoryCache) : ICommandHandler<CloneFromPersonalCommand, CloneFromPersonalResponse>
{
    public async Task<CloneFromPersonalResponse> Handle(
        CloneFromPersonalCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.CurrentUserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var currentOrgId = await organizationContextAccessor.GetCurrentOrganizationIdAsync();

        var isMember = await organizationAuthorizationService.IsMemberAsync(currentOrgId, userId);
        if (!isMember)
            throw new UnauthorizedAccessException("You are not a member of this organization.");

        // Verify current org is NOT personal
        var currentOrg = await organizationRepository.GetByIdAsync(currentOrgId, cancellationToken)
            ?? throw new InvalidOperationException("Current organization not found.");

        if (currentOrg.IsPersonal)
            throw new InvalidOperationException("Cannot clone payment method to a personal organization.");

        // Find the user's personal org
        var personalOrg = await organizationRepository.GetPersonalOrgForUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Personal organization not found.");

        // Get the personal org's Stripe customer ID
        var personalStripeCustomerId = await stripeCustomerRepository
            .GetStripeCustomerIdByInternalIdAsync(personalOrg.Id.Value.ToString(), cancellationToken)
            ?? throw new InvalidOperationException("Personal organization does not have a Stripe customer.");

        // Get the default payment method from personal customer
        var stripeClient = new StripeClient(options.Value.ApiKey);
        var customerService = new CustomerService(stripeClient);
        var paymentMethodService = new PaymentMethodService(stripeClient);

        var personalCustomer = await customerService.GetAsync(personalStripeCustomerId, cancellationToken: cancellationToken);
        var sourcePaymentMethodId = personalCustomer.InvoiceSettings?.DefaultPaymentMethodId;

        // Fall back to listing cards if no default is set
        if (string.IsNullOrEmpty(sourcePaymentMethodId))
        {
            var listOptions = new PaymentMethodListOptions
            {
                Customer = personalStripeCustomerId,
                Type = "card",
                Limit = 1
            };
            var methods = await paymentMethodService.ListAsync(listOptions, cancellationToken: cancellationToken);
            sourcePaymentMethodId = methods.Data.FirstOrDefault()?.Id;
        }

        if (string.IsNullOrEmpty(sourcePaymentMethodId))
            throw new InvalidOperationException("Personal organization has no payment method to clone.");

        // Get or create the destination org's Stripe customer
        var destStripeCustomerId = await currentStripeCustomerIdAccessor
            .GetOrCreateStripeCustomerIdForOrganization(currentOrgId);

        // Clone the payment method to the destination customer
        var createOptions = new PaymentMethodCreateOptions
        {
            Customer = destStripeCustomerId,
            PaymentMethod = sourcePaymentMethodId
        };
        var clonedPm = await paymentMethodService.CreateAsync(createOptions, cancellationToken: cancellationToken);

        // Set cloned PM as default on the destination customer
        var updateOptions = new CustomerUpdateOptions
        {
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = clonedPm.Id
            }
        };
        await customerService.UpdateAsync(destStripeCustomerId, updateOptions, cancellationToken: cancellationToken);

        // Evict the payment methods cache for this org
        memoryCache.Remove($"PaymentMethods-{currentOrgId.Value}");

        Log.Information(
            "Cloned payment method {SourcePmId} from personal org {PersonalOrgId} to org {DestOrgId} as {ClonedPmId}",
            sourcePaymentMethodId, personalOrg.Id, currentOrgId, clonedPm.Id);

        return new CloneFromPersonalResponse(new PaymentMethodDto
        {
            Id = clonedPm.Id,
            Type = clonedPm.Type,
            Brand = clonedPm.Card?.Brand ?? "",
            Last4 = clonedPm.Card?.Last4 ?? "",
            ExpiryMonth = (int?)clonedPm.Card?.ExpMonth ?? 0,
            ExpiryYear = (int?)clonedPm.Card?.ExpYear ?? 0,
            IsDefault = true
        });
    }
}

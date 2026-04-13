using Auth.Services;
using Core.MediatR;
using Microsoft.Extensions.Options;
using Organizations.Data.Repositories;
using Organizations.Services;
using Serilog;
using Stripe.Data.Repositories;
using Stripe.Options;

namespace Stripe.Endpoints.PaymentMethods.GetPersonalOrgHasPaymentMethod;

public sealed class GetPersonalOrgHasPaymentMethodCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IOrganizationContextAccessor organizationContextAccessor,
    IOrganizationRepository organizationRepository,
    IStripeCustomerRepository stripeCustomerRepository,
    IOptions<StripeOptions> options) : ICommandHandler<GetPersonalOrgHasPaymentMethodCommand, GetPersonalOrgHasPaymentMethodResponse>
{
    public async Task<GetPersonalOrgHasPaymentMethodResponse> Handle(
        GetPersonalOrgHasPaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.CurrentUserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var currentOrgId = await organizationContextAccessor.GetCurrentOrganizationIdAsync();

        // If current org is personal, no point offering to clone from self
        var currentOrg = await organizationRepository.GetByIdAsync(currentOrgId, cancellationToken);
        if (currentOrg is null || currentOrg.IsPersonal)
            return new GetPersonalOrgHasPaymentMethodResponse(false, null, null);

        // Find the user's personal org
        var personalOrg = await organizationRepository.GetPersonalOrgForUserAsync(userId, cancellationToken);
        if (personalOrg is null)
            return new GetPersonalOrgHasPaymentMethodResponse(false, null, null);

        // Get the personal org's Stripe customer ID
        var personalStripeCustomerId = await stripeCustomerRepository
            .GetStripeCustomerIdByInternalIdAsync(personalOrg.Id.Value.ToString(), cancellationToken);

        if (string.IsNullOrEmpty(personalStripeCustomerId))
            return new GetPersonalOrgHasPaymentMethodResponse(false, null, null);

        // List payment methods for the personal org's Stripe customer
        try
        {
            var stripeClient = new StripeClient(options.Value.ApiKey);
            var paymentMethodService = new PaymentMethodService(stripeClient);

            var listOptions = new PaymentMethodListOptions
            {
                Customer = personalStripeCustomerId,
                Type = "card",
                Limit = 1
            };

            var methods = await paymentMethodService.ListAsync(listOptions, cancellationToken: cancellationToken);
            var pm = methods.Data.FirstOrDefault();

            if (pm is null)
                return new GetPersonalOrgHasPaymentMethodResponse(false, null, null);

            return new GetPersonalOrgHasPaymentMethodResponse(
                true,
                pm.Card?.Last4,
                pm.Card?.Brand);
        }
        catch (StripeException ex)
        {
            Log.Warning(ex, "Failed to check personal org payment methods for user {UserId}", userId);
            return new GetPersonalOrgHasPaymentMethodResponse(false, null, null);
        }
    }
}

using Core.Extensions;
using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.AttachPaymentMethod;

public class AttachPaymentMethodCommandHandler : ICommandHandler<AttachPaymentMethodCommand, AttachPaymentMethodResponse>
{
    private readonly IStripeService _stripeService;

    public AttachPaymentMethodCommandHandler(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }

    public async Task<AttachPaymentMethodResponse> Handle(
        AttachPaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        var userPaymentMethods = await _stripeService.GetUserPaymentMethodsAsync(cancellationToken);

        var paymentMethod = await _stripeService.AddPaymentMethodAsync(
            request.PaymentMethodId,
            cancellationToken);

        //Remove old payment methods if the user already has one
        await userPaymentMethods.ApplyAsync(
            (p) => _stripeService.RemovePaymentMethodAsync(p.Id, cancellationToken));

        return new AttachPaymentMethodResponse(paymentMethod);
    }
}

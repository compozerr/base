using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.SetDefaultPaymentMethod;

public class SetDefaultPaymentMethodCommandHandler : ICommandHandler<SetDefaultPaymentMethodCommand, SetDefaultPaymentMethodResponse>
{
    private readonly IStripeService _stripeService;

    public SetDefaultPaymentMethodCommandHandler(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }

    public async Task<SetDefaultPaymentMethodResponse> Handle(
        SetDefaultPaymentMethodCommand request, 
        CancellationToken cancellationToken)
    {
        var paymentMethod = await _stripeService.SetDefaultPaymentMethodAsync(
            request.PaymentMethodId,
            cancellationToken);

        return new SetDefaultPaymentMethodResponse(paymentMethod);
    }
}

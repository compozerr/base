using Core.MediatR;
using MediatR;
using Stripe.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Stripe.Endpoints.PaymentMethods.RemovePaymentMethod;

public class RemovePaymentMethodCommandHandler : ICommandHandler<RemovePaymentMethodCommand, RemovePaymentMethodResponse>
{
    private readonly IStripeService _stripeService;

    public RemovePaymentMethodCommandHandler(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }

    public async Task<RemovePaymentMethodResponse> Handle(
        RemovePaymentMethodCommand request, 
        CancellationToken cancellationToken)
    {
        var result = await _stripeService.RemovePaymentMethodAsync(
            request.PaymentMethodId,
            cancellationToken);

        return new RemovePaymentMethodResponse(result);
    }
}

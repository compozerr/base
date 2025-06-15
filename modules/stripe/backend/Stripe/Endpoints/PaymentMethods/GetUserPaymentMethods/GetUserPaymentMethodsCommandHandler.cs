using Core.MediatR;
using MediatR;
using Stripe.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Stripe.Endpoints.PaymentMethods.GetUserPaymentMethods;

public class GetUserPaymentMethodsCommandHandler : ICommandHandler<GetUserPaymentMethodsCommand, GetUserPaymentMethodsResponse>
{
    private readonly IStripeService _stripeService;

    public GetUserPaymentMethodsCommandHandler(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }

    public async Task<GetUserPaymentMethodsResponse> Handle(
        GetUserPaymentMethodsCommand request, 
        CancellationToken cancellationToken)
    {
        var paymentMethods = await _stripeService.GetUserPaymentMethodsAsync(
            request.UserId, 
            cancellationToken);

        return new GetUserPaymentMethodsResponse(paymentMethods);
    }
}

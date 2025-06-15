using Core.MediatR;
using MediatR;
using Stripe.Services;
using System.Threading;
using System.Threading.Tasks;

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
        var paymentMethod = await _stripeService.AddPaymentMethodAsync(
            request.UserId,
            request.PaymentMethodId,
            cancellationToken);

        return new AttachPaymentMethodResponse(paymentMethod);
    }
}

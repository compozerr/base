using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.SetDefaultPaymentMethod;

public class SetDefaultPaymentMethodCommandHandler(
    IPaymentMethodsService paymentMethodsService
) : ICommandHandler<SetDefaultPaymentMethodCommand, SetDefaultPaymentMethodResponse>
{
    public async Task<SetDefaultPaymentMethodResponse> Handle(
        SetDefaultPaymentMethodCommand request,
        CancellationToken cancellationToken)
    {
        var paymentMethod = await paymentMethodsService.SetDefaultPaymentMethodAsync(
            request.PaymentMethodId,
            cancellationToken);

        return new SetDefaultPaymentMethodResponse(paymentMethod);
    }
}

using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.CreateSetupIntent;

public class CreateSetupIntentCommandHandler(
    IPaymentMethodsService paymentMethodsService) : ICommandHandler<CreateSetupIntentCommand, CreateSetupIntentResponse>
{
    public async Task<CreateSetupIntentResponse> Handle(
        CreateSetupIntentCommand command,
        CancellationToken cancellationToken)
    {
        var clientSecret = await paymentMethodsService.CreateSetupIntentAsync(cancellationToken);
        return new CreateSetupIntentResponse(clientSecret);
    }
}

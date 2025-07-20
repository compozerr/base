using MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.CreateSetupIntent;

public class CreateSetupIntentCommandHandler : IRequestHandler<CreateSetupIntentCommand, CreateSetupIntentResponse>
{
    private readonly IStripeService _stripeService;

    public CreateSetupIntentCommandHandler(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }

    public async Task<CreateSetupIntentResponse> Handle(CreateSetupIntentCommand request, CancellationToken cancellationToken)
    {
        var clientSecret = await _stripeService.CreateSetupIntentAsync(cancellationToken);
        return new CreateSetupIntentResponse(clientSecret);
    }
}

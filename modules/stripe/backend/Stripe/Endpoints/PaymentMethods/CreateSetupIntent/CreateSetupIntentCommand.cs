using MediatR;

namespace Stripe.Endpoints.PaymentMethods.CreateSetupIntent;

public record CreateSetupIntentCommand : IRequest<CreateSetupIntentResponse>;

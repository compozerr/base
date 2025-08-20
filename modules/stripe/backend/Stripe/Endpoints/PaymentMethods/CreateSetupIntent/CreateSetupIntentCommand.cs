using Core.MediatR;

namespace Stripe.Endpoints.PaymentMethods.CreateSetupIntent;

public record CreateSetupIntentCommand : ICommand<CreateSetupIntentResponse>;

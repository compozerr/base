using Core.MediatR;

namespace Stripe.Endpoints.Webhooks.ProcessWebhook;

public record ProcessWebhookCommand(
    string PayloadJson,
    string StripeSignature) : ICommand;

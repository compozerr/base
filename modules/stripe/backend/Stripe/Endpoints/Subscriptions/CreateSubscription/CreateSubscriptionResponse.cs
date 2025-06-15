namespace Stripe.Endpoints.Subscriptions.CreateSubscription;

public sealed record CreateSubscriptionResponse(
    string SessionId,
    string Url,
    string ClientReferenceId,
    string PaymentStatus);

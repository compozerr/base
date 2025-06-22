namespace Stripe.Endpoints.Subscriptions.UpsertSubscription;

public sealed record UpsertSubscriptionResponse(
    string SubscriptionId,
    string? CheckoutSessionId = null,
    string? CheckoutUrl = null,
    string? ClientReferenceId = null,
    string? PaymentStatus = null);

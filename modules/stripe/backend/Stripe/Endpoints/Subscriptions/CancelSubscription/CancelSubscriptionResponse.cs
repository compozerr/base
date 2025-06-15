namespace Stripe.Endpoints.Subscriptions.CancelSubscription;

public sealed record CancelSubscriptionResponse(
    string SubscriptionId,
    string Status,
    string? CanceledAt,
    bool CanceledImmediately);

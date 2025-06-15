namespace Stripe.Endpoints.UpdateSubscription;

public sealed record UpdateSubscriptionResponse(
    string SubscriptionId,
    string Status,
    decimal ProrationAmount,
    string? NextBillingDate,
    bool IsUpgrade);

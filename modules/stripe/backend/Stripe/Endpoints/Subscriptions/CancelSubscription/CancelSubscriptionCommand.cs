using Core.MediatR;

namespace Stripe.Endpoints.Subscriptions.CancelSubscription;

public sealed record CancelSubscriptionCommand(
    string SubscriptionId, 
    bool CancelImmediately) : ICommand<CancelSubscriptionResponse>;

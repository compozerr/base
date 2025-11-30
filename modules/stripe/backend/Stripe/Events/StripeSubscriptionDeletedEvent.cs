using Core.Abstractions;

namespace Stripe.Events;

public record StripeSubscriptionDeletedEvent(
    string SubscriptionId,
    string CustomerId) : IEvent;

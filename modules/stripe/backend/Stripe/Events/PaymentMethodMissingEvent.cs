using Core.Abstractions;

namespace Stripe.Events;

public record PaymentMethodMissingEvent(
    string CustomerId,
    int AttemptCount) : IEvent;

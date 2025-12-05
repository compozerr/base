using Core.Abstractions;
using Stripe.Data.Models;

namespace Stripe.Events;

public record PaymentMethodMissingEvent(
    string CustomerId,
    int AttemptCount,
    PaymentMethodMissingSaga Saga) : IEvent;

using Core.Abstractions;

namespace Stripe.Events;

public record StripeCustomerPaymentMethodAddedEvent(
    string CustomerId,
    string PaymentMethodId) : IEvent;

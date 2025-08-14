using Core.Abstractions;

namespace Stripe.Events;

public record StripeInvoicePaymentFailedEvent(
    string InvoiceId,
    string CustomerId,
    string SubscriptionId,
    decimal AmountDue,
    int AttemptCount,
    string? FailureReason,
    DateTime? NextPaymentAttempt) : IEvent;

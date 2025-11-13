using Core.Abstractions;

namespace Stripe.Events;

public record StripeInvoicePaymentFailedEvent(
    string InvoiceId,
    string CustomerId,
    string SubscriptionId,
    decimal AmountDue,
    string Currency,
    DateTime DueDate,
    int DaysOverdue,
    string PaymentLink,
    int AttemptCount,
    string? FailureReason,
    DateTime? NextPaymentAttempt) : IEvent;

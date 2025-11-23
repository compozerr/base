using Core.Abstractions;

namespace Stripe.Events;

public record StripeInvoicePaymentSucceededEvent(
    string InvoiceId,
    string CustomerId,
    string SubscriptionId,
    string InvoiceLink,
    decimal AmountPaid,
    string Currency,
    DateTime PaidAt) : IEvent;

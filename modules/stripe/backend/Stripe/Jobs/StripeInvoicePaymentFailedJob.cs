using Jobs;
using MediatR;
using Stripe.Events;

namespace Stripe.Jobs;

public class StripeInvoicePaymentFailedJob(
    IPublisher publisher) : JobBase<StripeInvoicePaymentFailedJob, StripeInvoicePaymentFailedEvent>
{
    public override string? GetDistributedLockKey(StripeInvoicePaymentFailedEvent @event)
        => $"stripe:invoice:payment_failed:{@event.InvoiceId}";

    public override Task ExecuteAsync(StripeInvoicePaymentFailedEvent @event)
        => publisher.Publish(@event);
}
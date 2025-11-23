using Jobs;
using MediatR;
using Stripe.Events;

namespace Stripe.Jobs;

public class StripeInvoicePaymentSucceededJob(
    IPublisher publisher) : JobBase<StripeInvoicePaymentSucceededJob, StripeInvoicePaymentSucceededEvent>
{
    public override string? GetDistributedLockKey(StripeInvoicePaymentSucceededEvent @event)
        => $"stripe:invoice:payment_succeeded:{@event.InvoiceId}";
    public override Task ExecuteAsync(StripeInvoicePaymentSucceededEvent @event)
        => publisher.Publish(@event);
}
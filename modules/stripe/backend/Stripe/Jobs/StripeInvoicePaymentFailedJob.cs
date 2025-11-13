using Jobs;
using MediatR;
using Stripe.Events;

namespace Stripe.Jobs;

public class StripeInvoicePaymentFailedJob(
    IPublisher publisher) : JobBase<StripeInvoicePaymentFailedJob, StripeInvoicePaymentFailedEvent>
{
    public override Task ExecuteAsync(StripeInvoicePaymentFailedEvent @event)
    => publisher.Publish(
            @event);
}
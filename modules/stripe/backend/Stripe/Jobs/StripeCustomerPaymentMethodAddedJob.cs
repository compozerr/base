using Jobs;
using MediatR;
using Stripe.Events;

namespace Stripe.Jobs;

public class StripeCustomerPaymentMethodAddedJob(
    IPublisher publisher) : JobBase<StripeCustomerPaymentMethodAddedJob, StripeCustomerPaymentMethodAddedEvent>
{
    public override string? GetDistributedLockKey(StripeCustomerPaymentMethodAddedEvent @event)
        => $"stripe:customer:payment_method_added:{@event.CustomerId}";

    public override Task ExecuteAsync(StripeCustomerPaymentMethodAddedEvent @event)
        => publisher.Publish(@event);
}

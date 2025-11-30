using Jobs;
using MediatR;
using Stripe.Events;

namespace Stripe.Jobs;

public class StripeSubscriptionDeletedJob(
    IPublisher publisher) : JobBase<StripeSubscriptionDeletedJob, StripeSubscriptionDeletedEvent>
{
    public override string? GetDistributedLockKey(StripeSubscriptionDeletedEvent @event)
        => $"stripe:subscription:deleted:{@event.SubscriptionId}";

    public override Task ExecuteAsync(StripeSubscriptionDeletedEvent @event)
        => publisher.Publish(@event);
}

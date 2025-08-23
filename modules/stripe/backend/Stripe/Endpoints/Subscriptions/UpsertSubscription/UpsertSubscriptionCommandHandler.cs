using Core.MediatR;
using Stripe.Services;
using MediatR;
using Stripe.Events;

namespace Stripe.Endpoints.Subscriptions.UpsertSubscription;

public sealed class UpsertSubscriptionCommandHandler(
    ISubscriptionsService subscriptionService,
    IPublisher publisher) : ICommandHandler<UpsertSubscriptionCommand, UpsertSubscriptionResponse>
{
    public async Task<UpsertSubscriptionResponse> Handle(UpsertSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var existingSubscriptions = await subscriptionService.GetSubscriptionsForUserAsync(cancellationToken);

        var existingSubscription = existingSubscriptions
            .FirstOrDefault(s => (s.Status == "active" || s.Status == "trialing") && s.ProjectId == command.ProjectId);

        if (existingSubscription != null)
        {
            var updatedSubscription = await subscriptionService.UpdateSubscriptionTierAsync(
                existingSubscription.Id,
                command.ProjectId,
                command.ServerTierId,
                cancellationToken);

            await publisher.Publish(
                new StripeSubscriptionUpdatedEvent(
                    SubscriptionId: updatedSubscription.Id,
                    ProjectId: command.ProjectId,
                    ServerTierId: command.ServerTierId),
                cancellationToken);

            return new UpsertSubscriptionResponse(
                SubscriptionId: updatedSubscription.Id);
        }
        else
        {
            var session = await subscriptionService.CreateSubscriptionTierAsync(
                command.ProjectId,
                command.ServerTierId,
                cancellationToken);

            await publisher.Publish(
                new StripeSubscriptionUpdatedEvent(
                    SubscriptionId: session.Id,
                    ProjectId: command.ProjectId,
                    ServerTierId: command.ServerTierId),
                cancellationToken);

            return new UpsertSubscriptionResponse(
                SubscriptionId: session.Id);
        }
    }
}

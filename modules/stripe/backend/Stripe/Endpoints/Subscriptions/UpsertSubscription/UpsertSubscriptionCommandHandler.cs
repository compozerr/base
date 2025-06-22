using Core.MediatR;
using Api.Abstractions;
using Stripe.Services;
using Core.Services;
using MediatR;
using Stripe.Events;

namespace Stripe.Endpoints.Subscriptions.UpsertSubscription;

public sealed class UpsertSubscriptionCommandHandler : ICommandHandler<UpsertSubscriptionCommand, UpsertSubscriptionResponse>
{
    private readonly IStripeService _stripeService;
    private readonly IFrontendLocation _frontendLocation;
    private readonly IPublisher _publisher;

    public UpsertSubscriptionCommandHandler(
        IStripeService stripeService,
        IFrontendLocation frontendLocation,
        IPublisher publisher)
    {
        _stripeService = stripeService;
        _frontendLocation = frontendLocation;
        _publisher = publisher;
    }

    public async Task<UpsertSubscriptionResponse> Handle(UpsertSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var existingSubscriptions = await _stripeService.GetSubscriptionsForUserAsync(cancellationToken);

        var existingSubscription = existingSubscriptions
            .FirstOrDefault(s => (s.Status == "active" || s.Status == "trialing") && s.ProjectId == command.ProjectId);

        if (existingSubscription != null)
        {
            var updatedSubscription = await _stripeService.UpdateSubscriptionTierAsync(
                existingSubscription.Id,
                command.ProjectId,
                command.ServerTierId,
                cancellationToken);

            await _publisher.Publish(
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
            var session = await _stripeService.CreateSubscriptionTierAsync(
                command.ProjectId,
                command.ServerTierId,
                cancellationToken);

            await _publisher.Publish(
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

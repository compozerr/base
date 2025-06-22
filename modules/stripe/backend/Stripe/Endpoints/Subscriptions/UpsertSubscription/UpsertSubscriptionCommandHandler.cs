using Core.MediatR;
using Api.Abstractions;
using Stripe.Services;
using Core.Services;
using MediatR;

namespace Stripe.Endpoints.Subscriptions.UpsertSubscription;

public sealed class UpsertSubscriptionCommandHandler : ICommandHandler<UpsertSubscriptionCommand, UpsertSubscriptionResponse>
{
    private readonly IStripeService _stripeService;
    private readonly IFrontendLocation _frontendLocation;
    private readonly ISender _mediator;

    public UpsertSubscriptionCommandHandler(
        IStripeService stripeService,
        IFrontendLocation frontendLocation,
        ISender mediator)
    {
        _stripeService = stripeService;
        _frontendLocation = frontendLocation;
        _mediator = mediator;
    }

    public async Task<UpsertSubscriptionResponse> Handle(UpsertSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var existingSubscriptions = await _stripeService.GetSubscriptionsForUserAsync(cancellationToken);

        var existingSubscription = existingSubscriptions
            .FirstOrDefault(s => s.Status == "active" || s.Status == "trialing" && s.ProjectId == command.ProjectId);

        if (existingSubscription != null)
        {
            var updatedSubscription = await _stripeService.UpdateSubscriptionTierAsync(
                existingSubscription.Id,
                command.ProjectId,
                command.ServerTierId,
                cancellationToken);

            await _mediator.Send(new ChangeTierCommand(
                ProjectId: command.ProjectId,
                Tier: command.ServerTierId.Value),
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

            return new UpsertSubscriptionResponse(
                SubscriptionId: session.Id);
        }
    }
}

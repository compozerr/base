using Core.Abstractions;
using MediatR;
using Stripe.Events;

namespace Api.Endpoints.Projects.Project.ChangeTier;

public sealed class ChangeTier_StripeSubscriptionUpdatedEventHandler(
    ISender sender) : IEventHandler<StripeSubscriptionUpdatedEvent>
{
    public async Task Handle(
        StripeSubscriptionUpdatedEvent @event,
        CancellationToken cancellationToken)
    {
        var command = new ChangeTierCommand(
            @event.ProjectId,
            @event.ServerTierId.Value);

        await sender.Send(command, cancellationToken);
    }
}
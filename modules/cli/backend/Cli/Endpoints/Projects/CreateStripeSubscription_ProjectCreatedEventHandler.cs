using Core.Abstractions;
using MediatR;
using Stripe.Endpoints.Subscriptions.UpsertSubscription;

namespace Cli.Endpoints.Projects;

public sealed class CreateStripeSubscription_ProjectCreatedEventHandler(
    ISender sender) : IDomainEventHandler<ProjectCreatedEvent>
{
    public async Task Handle(
        ProjectCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        var command = new UpsertSubscriptionCommand(
            notification.Entity.Id,
            notification.Entity.ServerTierId);

        await sender.Send(command, cancellationToken);
    }
}
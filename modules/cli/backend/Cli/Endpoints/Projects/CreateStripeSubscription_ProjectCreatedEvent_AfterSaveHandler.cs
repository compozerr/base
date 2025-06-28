using Core.Abstractions;
using MediatR;
using Stripe.Endpoints.Subscriptions.UpsertSubscription;

namespace Cli.Endpoints.Projects;

public sealed class CreateStripeSubscription_ProjectCreatedEvent_AfterSaveHandler(
    ISender sender) : IDomainEventHandler<ProjectCreatedEvent_AfterSave>
{
    public async Task Handle(
        ProjectCreatedEvent_AfterSave notification,
        CancellationToken cancellationToken)
    {
        var command = new UpsertSubscriptionCommand(
            notification.Entity.Id,
            notification.Entity.ServerTierId);

        await sender.Send(command, cancellationToken);
    }
}
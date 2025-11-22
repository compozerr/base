using Cli.Abstractions;
using Core.Abstractions;
using MediatR;
using Stripe.Endpoints.Subscriptions.UpsertSubscription;

namespace Cli.Endpoints.Projects;

public sealed class CreateStripeSubscription_ProjectCreatedEventHandler(
    ISender sender) : EntityDomainEventHandlerBase<ProjectCreatedEvent>
{
    protected override async Task HandleAfterSaveAsync(
        ProjectCreatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        if (domainEvent.Entity.Type != Api.Abstractions.ProjectType.Compozerr) return;

        var command = new UpsertSubscriptionCommand(
            domainEvent.Entity.Id,
            domainEvent.Entity.ServerTierId);

        await sender.Send(
            command,
            cancellationToken);
    }
}

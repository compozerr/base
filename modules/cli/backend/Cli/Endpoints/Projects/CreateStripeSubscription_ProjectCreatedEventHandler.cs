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
        var command = new UpsertSubscriptionCommand(
            domainEvent.Entity.Id,
            domainEvent.Entity.ServerTierId,
            "BLACKFRIDAY2025");

        await sender.Send(
            command,
            cancellationToken);
    }
}

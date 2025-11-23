using Api.Abstractions;
using Api.Features.N8n.Events;
using Core.Abstractions;
using Github.Services;
using MediatR;
using Stripe.Endpoints.Subscriptions.UpsertSubscription;

namespace Api.Features.N8n.EventHandlers;

public sealed class CreateStripeSubscription_N8nProjectCreatedEventHandler(
    ISender sender)
    : EntityDomainEventHandlerBase<N8nProjectCreatedEvent>
{
    protected override async Task HandleAfterSaveAsync(
        N8nProjectCreatedEvent domainEvent,
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

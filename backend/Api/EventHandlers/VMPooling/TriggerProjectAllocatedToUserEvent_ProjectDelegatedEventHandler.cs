using Api.Data;
using Cli.Abstractions;
using Core.Abstractions;
using MediatR;

namespace Api.EventHandlers.VMPooling;

public sealed class TriggerProjectAllocatedToUserEvent_ProjectDelegatedEventHandler(
    IPublisher publisher)
    : EntityDomainEventHandlerBase<ProjectDelegatedEvent>
{
    protected override Task HandleAfterSaveAsync(
        ProjectDelegatedEvent domainEvent,
        CancellationToken cancellationToken)
         => publisher.Publish(
            new ProjectAllocatedToUserEvent(
                domainEvent.Entity,
                domainEvent.Entity.UserId));
}

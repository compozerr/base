using Auth.Abstractions;
using Cli.Abstractions;
using Core.Abstractions;
using MediatR;

namespace Cli.Endpoints.Projects;

public sealed class TriggerProjectAllocatedToUserEvent_ProjectCreatedEventHandler(
    IPublisher publisher) : EntityDomainEventHandlerBase<ProjectCreatedEvent>
{
    protected override async Task HandleAfterSaveAsync(
        ProjectCreatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var didAllocateProjectToUser = domainEvent.Entity.UserId is not null
            && domainEvent.Entity.UserId != UserId.Empty;

        if (!didAllocateProjectToUser) return;

        await publisher.Publish(new ProjectAllocatedToUserEvent(domainEvent.Entity, domainEvent.Entity!.UserId!),
            cancellationToken);
    }
}

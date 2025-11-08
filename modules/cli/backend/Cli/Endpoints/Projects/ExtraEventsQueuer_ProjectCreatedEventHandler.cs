using Cli.Abstractions;
using Core.Abstractions;

namespace Cli.Endpoints.Projects;

public sealed class ExtraEventsQueuer_ProjectCreatedEventHandler : EntityDomainEventHandlerBase<ProjectCreatedEvent>
{
    protected override Task HandleBeforeSaveAsync(
        ProjectCreatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        domainEvent.Entity.QueueDomainEvent(
            new ProjectCreatedEvent_AfterSave(domainEvent.Entity));

        return Task.CompletedTask;
    }
}
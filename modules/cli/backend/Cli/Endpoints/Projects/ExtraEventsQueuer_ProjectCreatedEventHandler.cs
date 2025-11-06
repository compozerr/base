using Cli.Abstractions;
using Core.Abstractions;

namespace Cli.Endpoints.Projects;

public sealed class ExtraEventsQueuer_ProjectCreatedEventHandler : IDomainEventHandler<ProjectCreatedEvent>
{
    public Task Handle(
        ProjectCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        notification.Entity.QueueDomainEvent(
            new ProjectCreatedEvent_AfterSave(
                notification.Entity));

        return Task.CompletedTask;
    }
}
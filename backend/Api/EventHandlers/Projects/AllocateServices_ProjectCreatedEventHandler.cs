using Api.Abstractions;
using Api.Data;
using Cli.Abstractions;
using Core.Abstractions;

namespace Api.EventHandlers.Projects;

public sealed class AllocateServices_ProjectCreatedEventHandler : EntityDomainEventHandlerBase<ProjectCreatedEvent>
{
    protected override Task HandleBeforeSaveAsync(ProjectCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        domainEvent.Entity.ProjectServices = [.. GetDefaultServices(domainEvent.Entity.Id)];

        return Task.CompletedTask;
    }

    private static List<ProjectService> GetDefaultServices(ProjectId projectId)
    {
        return
        [
            new ProjectService
            {
                ProjectId = projectId,
                Name = "Frontend",
                Port = "1234",
                Protocol = "http",
                IsSystem = true
            },
            new ProjectService
            {
                ProjectId = projectId,
                Name = "Backend",
                Port = "1235",
                Protocol = "http",
                IsSystem = true
            }
        ];
    }
}

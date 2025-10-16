using Api.Abstractions;
using Api.Data;
using Cli.Endpoints.Projects;
using Core.Abstractions;

namespace Api.EventHandlers.Projects;

public sealed class AllocateServices_ProjectCreatedEventHandler : IDomainEventHandler<ProjectCreatedEvent>
{
    public Task Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        notification.Entity.ProjectServices = [.. GetDefaultServices(notification.Entity.Id)];

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
                IsSystem = true
            },
            new ProjectService
            {
                ProjectId = projectId,
                Name = "Backend",
                Port = "1235",
                IsSystem = true
            }
        ];
    }
}

using Api.Data;
using Core.Abstractions;

namespace Cli.Endpoints.Projects;

public sealed class AddProjectEnvironment_ProjectCreatedEventHandler : IDomainEventHandler<ProjectCreatedEvent>
{
    public Task Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        var defaultEnvironment = new ProjectEnvironment
        {
            ProjectId = notification.Entity.Id,
            Branches = ["main"],
            AutoDeploy = true
        };

        notification.Entity.ProjectEnvironments = [defaultEnvironment];

        return Task.CompletedTask;
    }
}
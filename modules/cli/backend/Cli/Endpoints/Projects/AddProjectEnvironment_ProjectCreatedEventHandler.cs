using Api.Data;
using Cli.Abstractions;
using Core.Abstractions;

namespace Cli.Endpoints.Projects;

public sealed class AddProjectEnvironment_ProjectCreatedEventHandler : EntityDomainEventHandlerBase<ProjectCreatedEvent>
{
    protected override Task HandleBeforeSaveAsync(ProjectCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        var defaultEnvironment = new ProjectEnvironment
        {
            ProjectId = domainEvent.Entity.Id,
            Branches = ["main"],
            AutoDeploy = true
        };

        domainEvent.Entity.ProjectEnvironments = [defaultEnvironment];

        return Task.CompletedTask;
    }
}
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
            AutoDeploy = true,
            ServerTierId = domainEvent.Entity.ServerTierId,
            // If the allocator handler has already run, the project's ServerId hint is populated
            // and we copy it onto the env (the load-bearing FK after Spec B). If it runs after us,
            // its handler will iterate ProjectEnvironments and overwrite ServerId on this env.
            ServerId = domainEvent.Entity.ServerId,
        };

        domainEvent.Entity.ProjectEnvironments = [defaultEnvironment];

        return Task.CompletedTask;
    }
}
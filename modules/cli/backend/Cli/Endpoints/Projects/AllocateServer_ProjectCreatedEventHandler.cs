using Cli.Abstractions;
using Core.Abstractions;

namespace Cli.Endpoints.Projects;

public sealed class AllocateServer_ProjectCreatedEventHandler(
    IServerAllocator serverAllocator) : EntityDomainEventHandlerBase<ProjectCreatedEvent>
{
    protected override async Task HandleBeforeSaveAsync(ProjectCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        var allocation = await serverAllocator.AllocateForLocationAsync(domainEvent.Entity.LocationId, cancellationToken);

        // Option B: keep Project.ServerId as a deprecated default-server hint while
        // ProjectEnvironment.ServerId becomes the load-bearing FK.
        domainEvent.Entity.ServerId = allocation.ServerId;

        if (allocation.ChangedToLocationId is { } changedToLocationId)
            domainEvent.Entity.LocationId = changedToLocationId;

        // Propagate the allocation to any envs that AddProjectEnvironment_ProjectCreatedEventHandler
        // may already have attached. Handler order across two BeforeSave subscribers is not
        // guaranteed by MediatR, so AddProjectEnvironment_ProjectCreatedEventHandler also copies
        // Project.ServerId onto the env it creates — whichever handler runs second writes the
        // final value, and both produce env.ServerId == project.ServerId.
        if (domainEvent.Entity.ProjectEnvironments is { } envs)
        {
            foreach (var env in envs)
            {
                env.ServerId = allocation.ServerId;
            }
        }
    }
}

using Api.Abstractions;
using Cli.Abstractions;
using Core.Abstractions;

namespace Api.Hosting.EventHandlers;

public sealed class SetProjectType_ProjectCreatedEventHandler : IDomainEventHandler<ProjectCreatedEvent>
{
    public Task Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        var projectType = DetermineProjectType(notification.Entity.RepoUri.ToString());

        // Set the project type directly on the entity
        // No need to save - this is a BeforeSaveChanges event
        notification.Entity.Type = projectType;

        return Task.CompletedTask;
    }

    private static ProjectType DetermineProjectType(string repoUrl)
    {
        // Check if it's a known template repository
        if (TemplateRepositories.IsTemplateRepository(repoUrl, out var projectType))
            return projectType ?? ProjectType.Compozerr;

        return ProjectType.Compozerr;
    }
}

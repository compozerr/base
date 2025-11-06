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
        var uri = new Uri(repoUrl);
        var repoPath = uri.AbsolutePath.TrimStart('/').TrimEnd('/');

        // Check if it's the n8n template repository
        if (repoPath.Equals("compozerr/n8n-template", StringComparison.OrdinalIgnoreCase) ||
            repoPath.Equals("compozerr/n8n-template.git", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectType.N8n;
        }

        return ProjectType.Compozerr;
    }
}

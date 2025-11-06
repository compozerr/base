using Api.Abstractions;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Projects.Services;
using Core.Abstractions;

namespace Api.Hosting.EventHandlers;

public sealed class SetProjectType_ProjectServiceUpsertedEventHandler(
    IProjectRepository projectRepository) : IDomainEventHandler<ProjectServiceUpsertedEvent>
{
    public async Task Handle(ProjectServiceUpsertedEvent notification, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(
            notification.Entity.ProjectId,
            cancellationToken) ?? throw new ArgumentException("Project not found");

        // Determine project type based on repository URL
        var projectType = DetermineProjectType(project.RepoUri.ToString());

        // Only update if the type has changed
        if (project.Type == projectType)
        {
            return;
        }

        project.Type = projectType;
        await projectRepository.UpdateAsync(project, cancellationToken);
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

using Api.Abstractions;
using Api.Data.Repositories;
using Auth.Services;
using Core.MediatR;

namespace Cli.Endpoints.Projects.UpdateTemplate;

public sealed class UpdateTemplateCommandHandler(
    IProjectRepository projectRepository,
    ICurrentUserAccessor currentUserAccessor) : ICommandHandler<UpdateTemplateCommand, UpdateTemplateResponse>
{
    public async Task<UpdateTemplateResponse> Handle(
        UpdateTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.CurrentUserId
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var project = await projectRepository.GetByIdAsync(command.ProjectId);

        if (project is null)
        {
            return new UpdateTemplateResponse(false, $"Project with ID {command.ProjectId} not found");
        }

        // Verify the project belongs to the current user
        if (project.UserId != userId)
        {
            return new UpdateTemplateResponse(false, "You do not have permission to update this project");
        }

        // Map CLI template type to ProjectType enum
        var projectType = MapTemplateType(command.ProjectType);
        if (projectType is null)
        {
            return new UpdateTemplateResponse(false, $"Unknown project type: {command.ProjectType}");
        }

        project.Type = projectType.Value;

        await projectRepository.UpdateAsync(project, cancellationToken);

        return new UpdateTemplateResponse(true, $"Project type updated to {projectType}");
    }

    /// <summary>
    /// Maps CLI template type strings to ProjectType enum values
    /// </summary>
    private static ProjectType? MapTemplateType(string cliTemplateType)
    {
        return cliTemplateType.ToLowerInvariant() switch
        {
            "dockerfile" => ProjectType.Dockerfile,
            "docker-compose" => ProjectType.DockerCompose,
            "compozerr-standard" => ProjectType.Compozerr,
            "compozerr" => ProjectType.Compozerr,
            "n8n" => ProjectType.N8n,
            "unknown" => ProjectType.Dockerfile, // Default to Dockerfile for unknown types
            _ => null
        };
    }
}

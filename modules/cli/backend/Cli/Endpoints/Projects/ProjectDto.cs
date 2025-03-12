using Api.Data;

namespace Cli.Endpoints.Projects;

public sealed record ProjectDto(
    string Name,
    string RepoUri,
    Guid UserId,
    Guid ServerId
)
{
    public static ProjectDto FromProject(Project project)
    {
        return new ProjectDto(
            project.Name,
            project.RepoUri.ToString(),
            project.UserId.Value,
            project.ServerId!.Value);
    }
}

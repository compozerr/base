using Api.Abstractions;
using Api.Data;

namespace Cli.Endpoints.Projects;

// DEPRECATED ServerId: post-Spec-B a project can span multiple servers via
// its environments. ServerId here is filled from the production env's
// ServerId (or the project's legacy ServerId hint when production hasn't been
// backfilled yet) for CLI wire-compat. s-b3 adds per-env data alongside this
// field; once the CLI's minimum version bumps, this field can drop.
public sealed record ProjectDto(
    string Name,
    string RepoUri,
    Guid UserId,
    Guid ServerId
)
{
    public static ProjectDto FromProject(Project project, ServerId? productionEnvServerId)
    {
        var effectiveServerId = productionEnvServerId
            ?? project.ProjectEnvironments?
                .FirstOrDefault(e => e.Type == EnvironmentType.Production)?
                .ServerId;

        return new ProjectDto(
            project.Name,
            project.RepoUri.ToString(),
            project.UserId.Value,
            (effectiveServerId ?? throw new InvalidOperationException("Project has no allocated server")).Value);
    }
}

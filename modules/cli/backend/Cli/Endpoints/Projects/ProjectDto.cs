using Api.Abstractions;
using Api.Data;

namespace Cli.Endpoints.Projects;

// DEPRECATED ServerId: post-Spec-B a project can span multiple servers via
// its environments. ServerId here is filled from the production env's
// ServerId (or the project's legacy ServerId hint when production hasn't been
// backfilled yet) for CLI wire-compat. The Environments list is the
// authoritative source — once the CLI's minimum version bumps, ServerId can
// drop.
public sealed record ProjectDto(
    string Name,
    string RepoUri,
    Guid UserId,
    Guid ServerId,
    IReadOnlyList<ProjectEnvironmentServerDto> Environments
)
{
    public static ProjectDto FromProject(Project project, ServerId? productionEnvServerId)
    {
        var effectiveServerId = productionEnvServerId
            ?? project.ProjectEnvironments?
                .FirstOrDefault(e => e.Type == EnvironmentType.Production)?
                .ServerId;

        var envs = project.ProjectEnvironments?
            .OrderBy(e => e.Type)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Select(e => new ProjectEnvironmentServerDto(
                e.Id.Value,
                e.Name,
                e.Type.ToString(),
                e.ServerId?.Value,
                e.Server?.HostName))
            .ToList()
            ?? [];

        return new ProjectDto(
            project.Name,
            project.RepoUri.ToString(),
            project.UserId.Value,
            (effectiveServerId ?? throw new InvalidOperationException("Project has no allocated server")).Value,
            envs);
    }
}

public sealed record ProjectEnvironmentServerDto(
    Guid EnvironmentId,
    string? EnvironmentName,
    string EnvironmentType,
    Guid? ServerId,
    string? ServerHostName);

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
    Guid? ServerId,
    IReadOnlyList<ProjectEnvironmentServerDto> Environments
)
{
    public static ProjectDto FromProject(Project project, ServerId? productionEnvServerId)
    {
        // Fall back through prod-env ServerId then the project's legacy ServerId
        // hint, which is exactly the transition state (zero envs, or a prod env
        // not yet backfilled) this deprecated field exists to cover. If all
        // three are null, emit null rather than throwing — old CLIs treat the
        // field as nullable and the Environments list is authoritative anyway.
        var effectiveServerId = productionEnvServerId
            ?? project.ProjectEnvironments?
                .FirstOrDefault(e => e.Type == EnvironmentType.Production)?
                .ServerId
            ?? project.ServerId;

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
            effectiveServerId?.Value,
            envs);
    }
}

public sealed record ProjectEnvironmentServerDto(
    Guid EnvironmentId,
    string? EnvironmentName,
    string EnvironmentType,
    Guid? ServerId,
    string? ServerHostName);

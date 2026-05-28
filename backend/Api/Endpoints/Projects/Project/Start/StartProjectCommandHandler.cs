using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Project.Start;

public sealed class StartProjectCommandHandler(
    ApiDbContext context,
    IEnvironmentTargetResolver environmentTargetResolver) : ICommandHandler<StartProjectCommand, StartProjectResponse>
{
    public async Task<StartProjectResponse> Handle(StartProjectCommand command, CancellationToken cancellationToken = default)
    {
        var envs = await GetTargetEnvsAsync(command, cancellationToken);

        foreach (var env in envs)
        {
            var server = await environmentTargetResolver.TryGetServerAsync(env.Id, cancellationToken);
            if (server is null) continue;

            var hostingApi = await environmentTargetResolver.GetHostingApiAsync(env.Id, cancellationToken);

            // Preview env VMs are keyed by env name in the VM identifier
            // (HostingApi.BuildVmIdentifier); production uses the bare project
            // id. Derive the name from the env's Type so a project-wide fan-out
            // starts each preview env on its own server — not all as production.
            var environmentName = env.Type == EnvironmentType.Production ? null : env.Name;
            await hostingApi.StartProjectAsync(command.ProjectId, environmentName);
        }

        return new();
    }

    // Project-wide Start (no env name) fans out across all envs so projects
    // with envs on multiple servers are fully started. Per-env Start picks
    // the matching env only.
    private async Task<List<(ProjectEnvironmentId Id, string? Name, EnvironmentType Type)>> GetTargetEnvsAsync(
        StartProjectCommand command, CancellationToken cancellationToken)
    {
        var query = context.ProjectEnvironments
            .Where(e => e.ProjectId == command.ProjectId);

        if (command.EnvironmentName is { } name)
            query = query.Where(e => e.Name == name);

        var rows = await query
            .Select(e => new { e.Id, e.Name, e.Type })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Id, r.Name, r.Type)).ToList();
    }
}

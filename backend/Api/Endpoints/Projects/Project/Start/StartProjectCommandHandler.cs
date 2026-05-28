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
        var envIds = await GetTargetEnvIdsAsync(command, cancellationToken);

        foreach (var envId in envIds)
        {
            var server = await environmentTargetResolver.TryGetServerAsync(envId, cancellationToken);
            if (server is null) continue;

            var hostingApi = await environmentTargetResolver.GetHostingApiAsync(envId, cancellationToken);
            await hostingApi.StartProjectAsync(command.ProjectId, command.EnvironmentName);
        }

        return new();
    }

    // Project-wide Start (no env name) fans out across all envs so projects
    // with envs on multiple servers are fully started. Per-env Start picks
    // the matching env only.
    private async Task<List<ProjectEnvironmentId>> GetTargetEnvIdsAsync(
        StartProjectCommand command, CancellationToken cancellationToken)
    {
        if (command.EnvironmentName is { } name)
        {
            return await context.ProjectEnvironments
                .Where(e => e.ProjectId == command.ProjectId && e.Name == name)
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);
        }

        return await context.ProjectEnvironments
            .Where(e => e.ProjectId == command.ProjectId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }
}

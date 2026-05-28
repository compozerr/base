using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.Stop;

public sealed class StopProjectCommandHandler(
    IEnvironmentTargetResolver environmentTargetResolver) : ICommandHandler<StopProjectCommand, StopProjectResponse>
{
    public async Task<StopProjectResponse> Handle(StopProjectCommand command, CancellationToken cancellationToken = default)
    {
        // Stop project = stop on every server hosting any env of this project.
        // Distinct fan-out over servers (not envs) keeps it idempotent when
        // multiple envs share a server.
        var hostingApis = await environmentTargetResolver
            .GetDistinctHostingApisForProjectAsync(command.ProjectId, cancellationToken);

        foreach (var hostingApi in hostingApis)
        {
            await hostingApi.StopProjectAsync(command.ProjectId);
        }

        return new();
    }
}

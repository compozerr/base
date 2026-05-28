using Api.Data;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Services.ChangeProtocol;

public sealed class ChangeProtocolCommandHandler(
    ApiDbContext context,
    IEnvironmentTargetResolver environmentTargetResolver) : ICommandHandler<ChangeProtocolCommand, ChangeProtocolResponse>
{
    public async Task<ChangeProtocolResponse> Handle(
        ChangeProtocolCommand command,
        CancellationToken cancellationToken)
    {
        var service = await context.ProjectServices
            .FirstOrDefaultAsync(s => s.ProjectId == command.ProjectId && s.Name == command.ServiceName, cancellationToken)
            ?? throw new InvalidOperationException($"Service '{command.ServiceName}' not found for project");

        service.Protocol = command.Protocol;
        await context.SaveChangesAsync(cancellationToken);

        // Push domain refresh to the env(s) that host this service. When the
        // service is env-scoped, just push to that env's server; otherwise
        // (legacy/project-wide service) fan out across every server hosting
        // any env of the project so all Traefik configs reflect the change.
        if (service.ProjectEnvironmentId is { } envId)
        {
            var hostingApi = await environmentTargetResolver.GetHostingApiAsync(envId, cancellationToken);
            await hostingApi.UpdateDomainsForProjectAsync(command.ProjectId);
        }
        else
        {
            var hostingApis = await environmentTargetResolver
                .GetDistinctHostingApisForProjectAsync(command.ProjectId, cancellationToken);
            foreach (var hostingApi in hostingApis)
            {
                await hostingApi.UpdateDomainsForProjectAsync(command.ProjectId);
            }
        }

        return new ChangeProtocolResponse(service.Protocol);
    }
}

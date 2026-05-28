using Api.Data;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Services.UnexposeTcp;

public sealed class UnexposeTcpCommandHandler(
    ApiDbContext context,
    IEnvironmentTargetResolver environmentTargetResolver) : ICommandHandler<UnexposeTcpCommand, UnexposeTcpResponse>
{
    public async Task<UnexposeTcpResponse> Handle(
        UnexposeTcpCommand command,
        CancellationToken cancellationToken)
    {
        var allocation = await context.TcpPortAllocations
            .FirstOrDefaultAsync(a => a.ProjectId == command.ProjectId && a.ServiceName == command.ServiceName, cancellationToken)
            ?? throw new InvalidOperationException($"No TCP allocation found for service '{command.ServiceName}'");

        var serviceEnvId = await context.ProjectServices
            .Where(s => s.ProjectId == command.ProjectId && s.Name == command.ServiceName)
            .Select(s => s.ProjectEnvironmentId)
            .FirstOrDefaultAsync(cancellationToken);

        context.TcpPortAllocations.Remove(allocation);
        await context.SaveChangesAsync(cancellationToken);

        if (serviceEnvId is { } envId)
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

        return new();
    }
}

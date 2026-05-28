using Api.Data;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Services.ExposeTcp;

public sealed class ExposeTcpCommandHandler(
    ApiDbContext context,
    IEnvironmentTargetResolver environmentTargetResolver) : ICommandHandler<ExposeTcpCommand, ExposeTcpResponse>
{
    private const int PortRangeStart = 10000;
    private const int PortRangeEnd = 10999;
    private const int MaxRetries = 3;

    public async Task<ExposeTcpResponse> Handle(
        ExposeTcpCommand command,
        CancellationToken cancellationToken)
    {
        var service = await context.ProjectServices
            .FirstOrDefaultAsync(s => s.ProjectId == command.ProjectId && s.Name == command.ServiceName, cancellationToken)
            ?? throw new InvalidOperationException($"Service '{command.ServiceName}' not found for project");

        // TCP exposure is per-service and therefore per-env. The hosting server
        // is the one that runs the env hosting the service — same rule for both
        // VmIp (returned to Traefik) and ApiDomain (returned to the client).
        var server = await environmentTargetResolver
            .GetServerForServiceAsync(command.ProjectId, command.ServiceName, cancellationToken);

        var existing = await context.TcpPortAllocations
            .FirstOrDefaultAsync(a => a.ProjectId == command.ProjectId && a.ServiceName == command.ServiceName, cancellationToken);

        if (existing is not null)
        {
            await PushDomainsUpdateAsync(service, command, cancellationToken);
            return new ExposeTcpResponse(existing.AllocatedPort, server.ApiDomain);
        }

        var allocation = await AllocatePortWithRetry(command, service, server.Ip, cancellationToken);

        try
        {
            await PushDomainsUpdateAsync(service, command, cancellationToken);
        }
        catch
        {
            context.TcpPortAllocations.Remove(allocation);
            await context.SaveChangesAsync(cancellationToken);
            throw;
        }

        return new ExposeTcpResponse(allocation.AllocatedPort, server.ApiDomain);
    }

    private async Task PushDomainsUpdateAsync(
        ProjectService service,
        ExposeTcpCommand command,
        CancellationToken cancellationToken)
    {
        if (service.ProjectEnvironmentId is { } envId)
        {
            var hostingApi = await environmentTargetResolver.GetHostingApiAsync(envId, cancellationToken);
            await hostingApi.UpdateDomainsForProjectAsync(command.ProjectId);
            return;
        }

        var hostingApis = await environmentTargetResolver
            .GetDistinctHostingApisForProjectAsync(command.ProjectId, cancellationToken);
        foreach (var hostingApi in hostingApis)
        {
            await hostingApi.UpdateDomainsForProjectAsync(command.ProjectId);
        }
    }

    private async Task<TcpPortAllocation> AllocatePortWithRetry(
        ExposeTcpCommand command,
        ProjectService service,
        string vmIp,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var allocatedPorts = (await context.TcpPortAllocations
                .Select(a => a.AllocatedPort)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var nextPort = Enumerable.Range(PortRangeStart, PortRangeEnd - PortRangeStart + 1)
                .FirstOrDefault(p => !allocatedPorts.Contains(p));

            if (nextPort == 0)
                throw new InvalidOperationException("No available TCP ports in range 10000-10999");

            var allocation = new TcpPortAllocation
            {
                ProjectId = command.ProjectId,
                ServiceName = command.ServiceName,
                InternalPort = service.Port,
                AllocatedPort = nextPort,
                VmIp = vmIp
            };

            context.TcpPortAllocations.Add(allocation);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return allocation;
            }
            catch (DbUpdateException) when (attempt < MaxRetries - 1)
            {
                context.Entry(allocation).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("Failed to allocate TCP port after multiple retries due to concurrent allocation conflicts");
    }
}

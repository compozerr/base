using Api.Data;
using Api.Hosting.Services;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Services.ExposeTcp;

public sealed class ExposeTcpCommandHandler(
    ApiDbContext context,
    IHostingApiFactory hostingApiFactory) : ICommandHandler<ExposeTcpCommand, ExposeTcpResponse>
{
    private const int PortRangeStart = 10000;
    private const int PortRangeEnd = 10999;
    private const int MaxRetries = 3;

    public async Task<ExposeTcpResponse> Handle(
        ExposeTcpCommand command,
        CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .Include(p => p.Server)
            .FirstOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project with ID {command.ProjectId} not found");

        if (project.ServerId is null || project.Server is null)
            throw new InvalidOperationException("Project has no assigned server");

        var service = await context.ProjectServices
            .FirstOrDefaultAsync(s => s.ProjectId == command.ProjectId && s.Name == command.ServiceName, cancellationToken)
            ?? throw new InvalidOperationException($"Service '{command.ServiceName}' not found for project");

        var existing = await context.TcpPortAllocations
            .FirstOrDefaultAsync(a => a.ProjectId == command.ProjectId && a.ServiceName == command.ServiceName, cancellationToken);

        if (existing is not null)
        {
            var hostingApiExisting = await hostingApiFactory.GetHostingApiAsync(project.ServerId!);
            await hostingApiExisting.UpdateTcpProxyAsync(command.ProjectId);
            return new ExposeTcpResponse(existing.AllocatedPort, project.Server.ApiDomain);
        }

        var allocation = await AllocatePortWithRetry(command, service, project, cancellationToken);

        try
        {
            var hostingApi = await hostingApiFactory.GetHostingApiAsync(project.ServerId!);
            await hostingApi.UpdateTcpProxyAsync(command.ProjectId);
        }
        catch
        {
            context.TcpPortAllocations.Remove(allocation);
            await context.SaveChangesAsync(cancellationToken);
            throw;
        }

        return new ExposeTcpResponse(allocation.AllocatedPort, project.Server.ApiDomain);
    }

    private async Task<TcpPortAllocation> AllocatePortWithRetry(
        ExposeTcpCommand command,
        ProjectService service,
        Api.Data.Project project,
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
                VmIp = project.Server!.Ip
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

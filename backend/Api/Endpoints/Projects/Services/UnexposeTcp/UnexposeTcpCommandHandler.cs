using Api.Data;
using Api.Hosting.Services;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Services.UnexposeTcp;

public sealed class UnexposeTcpCommandHandler(
    ApiDbContext context,
    IHostingApiFactory hostingApiFactory) : ICommandHandler<UnexposeTcpCommand, UnexposeTcpResponse>
{
    public async Task<UnexposeTcpResponse> Handle(
        UnexposeTcpCommand command,
        CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .FirstOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project with ID {command.ProjectId} not found");

        if (project.ServerId is null)
            throw new InvalidOperationException("Project has no assigned server");

        var allocation = await context.TcpPortAllocations
            .FirstOrDefaultAsync(a => a.ProjectId == command.ProjectId && a.ServiceName == command.ServiceName, cancellationToken)
            ?? throw new InvalidOperationException($"No TCP allocation found for service '{command.ServiceName}'");

        context.TcpPortAllocations.Remove(allocation);
        await context.SaveChangesAsync(cancellationToken);

        var hostingApi = await hostingApiFactory.GetHostingApiAsync(project.ServerId!);
        await hostingApi.UpdateDomainsForProjectAsync(command.ProjectId);

        return new();
    }
}

using Api.Data;
using Api.Hosting.Services;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Services.ChangeProtocol;

public sealed class ChangeProtocolCommandHandler(
    ApiDbContext context,
    IHostingApiFactory hostingApiFactory) : ICommandHandler<ChangeProtocolCommand, ChangeProtocolResponse>
{
    public async Task<ChangeProtocolResponse> Handle(
        ChangeProtocolCommand command,
        CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .FirstOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project with ID {command.ProjectId} not found");

        var service = await context.ProjectServices
            .FirstOrDefaultAsync(s => s.ProjectId == command.ProjectId && s.Name == command.ServiceName, cancellationToken)
            ?? throw new InvalidOperationException($"Service '{command.ServiceName}' not found for project");

        service.Protocol = command.Protocol;
        await context.SaveChangesAsync(cancellationToken);

        if (project.ServerId is not null)
        {
            var hostingApi = await hostingApiFactory.GetHostingApiAsync(project.ServerId);
            await hostingApi.UpdateDomainsForProjectAsync(command.ProjectId);
        }

        return new ChangeProtocolResponse(service.Protocol);
    }
}

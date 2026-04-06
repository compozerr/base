using Api.Data;
using Api.Endpoints.Projects.Services.Get;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Environments.Services;

public sealed class GetEnvironmentServicesCommandHandler(ApiDbContext context)
    : ICommandHandler<GetEnvironmentServicesCommand, GetProjectServicesResponse>
{
    public async Task<GetProjectServicesResponse> Handle(
        GetEnvironmentServicesCommand command,
        CancellationToken cancellationToken)
    {
        var serverApiDomain = await context.Projects
            .Where(p => p.Id == command.ProjectId && p.ServerId != null)
            .Select(p => p.Server!.ApiDomain)
            .FirstOrDefaultAsync(cancellationToken);

        var allocations = await context.TcpPortAllocations
            .Where(a => a.ProjectId == command.ProjectId)
            .ToDictionaryAsync(a => a.ServiceName, a => a.AllocatedPort, cancellationToken);

        var services = await context.ProjectServices
            .Where(s => s.ProjectId == command.ProjectId && s.ProjectEnvironmentId == command.EnvironmentId)
            .OrderByDescending(s => s.IsSystem)
            .ThenBy(s => s.Name)
            .Select(s => new { s.Name, s.Port, s.Protocol, s.IsSystem })
            .ToListAsync(cancellationToken);

        var dtos = services.Select(s =>
        {
            var hasTcpAllocation = allocations.TryGetValue(s.Name, out var allocatedPort);
            return new ProjectServiceDto(
                s.Name,
                s.Port,
                s.Protocol,
                s.IsSystem,
                hasTcpAllocation ? allocatedPort : null,
                hasTcpAllocation ? serverApiDomain : null);
        }).ToList();

        return new GetProjectServicesResponse(dtos);
    }
}

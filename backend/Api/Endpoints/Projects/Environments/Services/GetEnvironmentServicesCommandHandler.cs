using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Endpoints.Projects.Services.Get;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Environments.Services;

public sealed class GetEnvironmentServicesCommandHandler(
    ApiDbContext context,
    IEnvironmentTargetResolver environmentTargetResolver)
    : ICommandHandler<GetEnvironmentServicesCommand, GetProjectServicesResponse>
{
    public async Task<GetProjectServicesResponse> Handle(
        GetEnvironmentServicesCommand command,
        CancellationToken cancellationToken)
    {
        string? serverApiDomain = null;
        try
        {
            var server = await environmentTargetResolver.GetServerAsync(command.EnvironmentId, cancellationToken);
            serverApiDomain = server.ApiDomain;
        }
        catch (ServerNotFoundException)
        {
            // Env has no server allocated yet — services are still returned,
            // but TCP-allocation rows lack a Host (matches prior null behaviour
            // when no project server was assigned).
        }

        var allocations = await context.TcpPortAllocations
            .Where(a => a.ProjectId == command.ProjectId)
            .ToDictionaryAsync(a => a.ServiceName, a => a.AllocatedPort, cancellationToken);

        var services = await context.ProjectServices
            .Where(s => s.ProjectId == command.ProjectId
                         && s.ProjectEnvironmentId == command.EnvironmentId
                         && s.DeletedAtUtc == null)
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

using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Services.Get;

public sealed class GetProjectServicesCommandHandler(
    ApiDbContext context,
    IEnvironmentTargetResolver environmentTargetResolver)
    : ICommandHandler<GetProjectServicesCommand, GetProjectServicesResponse>
{
    public async Task<GetProjectServicesResponse> Handle(
        GetProjectServicesCommand command,
        CancellationToken cancellationToken)
    {
        var allocations = await context.TcpPortAllocations
            .Where(a => a.ProjectId == command.ProjectId)
            .ToDictionaryAsync(a => a.ServiceName, a => a.AllocatedPort, cancellationToken);

        var services = await context.ProjectServices
            .Where(s => s.ProjectId == command.ProjectId && s.DeletedAtUtc == null)
            .OrderByDescending(s => s.IsSystem)
            .ThenBy(s => s.Name)
            .Select(s => new { s.Name, s.Port, s.Protocol, s.IsSystem, s.ProjectEnvironmentId })
            .ToListAsync(cancellationToken);

        // Each service row carries its env's server's ApiDomain so a project-wide
        // list correctly surfaces TCP hosts when envs live on different servers.
        // Resolution is cached per env id within this handler call.
        var apiDomainByEnv = new Dictionary<ProjectEnvironmentId, string?>();

        async Task<string?> ResolveApiDomainAsync(ProjectEnvironmentId? envId)
        {
            if (envId is null) return null;
            if (apiDomainByEnv.TryGetValue(envId, out var cached)) return cached;

            try
            {
                var server = await environmentTargetResolver.GetServerAsync(envId, cancellationToken);
                apiDomainByEnv[envId] = server.ApiDomain;
                return server.ApiDomain;
            }
            catch (ServerNotFoundException)
            {
                apiDomainByEnv[envId] = null;
                return null;
            }
        }

        var dtos = new List<ProjectServiceDto>(services.Count);
        foreach (var s in services)
        {
            var hasTcpAllocation = allocations.TryGetValue(s.Name, out var allocatedPort);
            var apiDomain = hasTcpAllocation ? await ResolveApiDomainAsync(s.ProjectEnvironmentId) : null;
            dtos.Add(new ProjectServiceDto(
                s.Name,
                s.Port,
                s.Protocol,
                s.IsSystem,
                hasTcpAllocation ? allocatedPort : null,
                apiDomain));
        }

        return new GetProjectServicesResponse(dtos);
    }
}

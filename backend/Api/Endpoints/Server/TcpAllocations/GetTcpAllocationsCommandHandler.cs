using Api.Data;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Server.TcpAllocations;

public sealed class GetTcpAllocationsCommandHandler(ApiDbContext context)
    : ICommandHandler<GetTcpAllocationsCommand, GetTcpAllocationsResponse>
{
    public async Task<GetTcpAllocationsResponse> Handle(
        GetTcpAllocationsCommand command,
        CancellationToken cancellationToken)
    {
        // Post-Spec-B: an env's server is what determines whether its TCP
        // allocations belong to this server, not the project-level hint.
        // A project can span servers, and a single server should only return
        // TCP allocations for envs it actually hosts.
        var projectIds = await context.ProjectEnvironments
            .Where(e => e.ServerId == command.ServerId)
            .Select(e => e.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allocations = await context.TcpPortAllocations
            .Where(a => projectIds.Contains(a.ProjectId))
            .ToListAsync(cancellationToken);

        var dtos = allocations.Select(a => new TcpAllocationDto(
            a.ProjectId.Value.ToString(),
            a.ServiceName,
            a.InternalPort,
            a.AllocatedPort,
            a.VmIp)).ToList();

        return new GetTcpAllocationsResponse(dtos);
    }
}

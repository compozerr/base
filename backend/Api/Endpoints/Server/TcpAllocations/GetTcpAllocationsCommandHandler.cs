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
        var projectIds = await context.Projects
            .Where(p => p.ServerId == command.ServerId)
            .Select(p => p.Id)
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

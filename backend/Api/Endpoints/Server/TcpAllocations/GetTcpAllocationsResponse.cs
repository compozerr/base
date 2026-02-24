namespace Api.Endpoints.Server.TcpAllocations;

public sealed record TcpAllocationDto(
    string ProjectId,
    string ServiceName,
    string InternalPort,
    int AllocatedPort,
    string VmIp);

public sealed record GetTcpAllocationsResponse(
    List<TcpAllocationDto> Allocations);

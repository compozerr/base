using Api.Data;

namespace Api.Endpoints.Projects.Services.Get;

public sealed record ProjectServiceDto(
    string Name,
    string Port,
    Protocol Protocol,
    bool IsSystem,
    int? AllocatedTcpPort,
    string? TcpHost);

public sealed record GetProjectServicesResponse(
    List<ProjectServiceDto> Services);

namespace Api.Endpoints.Projects.Services.Get;

public sealed record ProjectServiceDto(
    string Name,
    string Port,
    bool IsSystem);

public sealed record GetProjectServicesResponse(
    List<ProjectServiceDto> Services);

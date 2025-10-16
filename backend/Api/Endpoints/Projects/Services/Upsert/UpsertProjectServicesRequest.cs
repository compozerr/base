namespace Api.Endpoints.Projects.Services.Upsert;

public sealed record ServiceInfo(
    string Name,
    string Port);

public sealed record UpsertProjectServicesRequest(
    List<ServiceInfo> Services);

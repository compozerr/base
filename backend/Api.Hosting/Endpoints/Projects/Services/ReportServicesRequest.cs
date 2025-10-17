namespace Api.Hosting.Endpoints.Projects.Services;

public sealed record ServicePortInfo(
    string Name,
    string Port,
    string Protocol);

public sealed record ReportServicesRequest(
    List<ServicePortInfo> Services);

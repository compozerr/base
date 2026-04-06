using Api.Abstractions;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Projects.Services;

public sealed record ReportEnvironmentServicesCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId,
    List<ServicePortInfo> Services) : ICommand<ReportServicesResponse>;

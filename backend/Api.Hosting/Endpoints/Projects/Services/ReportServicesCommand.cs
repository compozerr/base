using Api.Abstractions;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Projects.Services;

public sealed record ReportServicesCommand(
    ProjectId ProjectId,
    List<ServicePortInfo> Services) : ICommand<ReportServicesResponse>;

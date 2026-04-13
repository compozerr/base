using Api.Abstractions;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Projects.Config;

public sealed record ReportProjectConfigCommand(
    ProjectId ProjectId,
    string Name) : ICommand<ReportProjectConfigResponse>;

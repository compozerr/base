using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Services.GetServiceLogs;

public sealed record GetServiceLogsCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId,
    string ServiceName,
    int Tail = 200) : ICommand<GetServiceLogsResponse>;

public sealed record GetServiceLogsResponse(string Logs, int ExitCode);

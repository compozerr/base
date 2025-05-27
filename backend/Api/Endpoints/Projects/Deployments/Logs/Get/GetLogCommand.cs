using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Deployments.Logs.Get;

public sealed record GetLogCommand(
    ProjectId ProjectId,
    DeploymentId DeploymentId) : ICommand<List<LogEntry>>;

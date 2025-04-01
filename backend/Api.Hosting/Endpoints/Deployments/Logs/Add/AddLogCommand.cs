using Api.Abstractions;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

public sealed record AddLogCommand(
    DeploymentId DeploymentId,
    string Log,
    LogLevel Level) : ICommand<AddLogResponse>;

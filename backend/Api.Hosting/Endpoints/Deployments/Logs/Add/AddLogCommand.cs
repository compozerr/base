using Core.MediatR;

namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

public sealed record AddLogCommand(
    Guid DeploymentId,
    string Log,
    LogLevel Level) : ICommand<AddLogResponse>;

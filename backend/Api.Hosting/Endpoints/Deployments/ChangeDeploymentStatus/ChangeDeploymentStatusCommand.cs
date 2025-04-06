using Api.Abstractions;
using Api.Data;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;

public sealed record ChangeDeploymentStatusCommand(
    DeploymentId DeploymentId,
    DeploymentStatus Status) : ICommand<ChangeDeploymentStatusResponse>;

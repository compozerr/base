using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Deployments.RedeployDeployment;

public sealed record RedeployDeploymentCommand(
	DeploymentId DeploymentId) : ICommand<RedeployDeploymentResponse>;

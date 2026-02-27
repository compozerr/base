using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Deployments.CancelDeployment;

public sealed record CancelDeploymentCommand(
	ProjectId ProjectId,
	DeploymentId DeploymentId) : ICommand<CancelDeploymentResponse>;

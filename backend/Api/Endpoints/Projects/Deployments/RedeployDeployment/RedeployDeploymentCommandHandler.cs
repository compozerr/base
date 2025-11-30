using Api.Abstractions;
using Api.Data.Repositories;
using Core.MediatR;
using MediatR;

namespace Api.Endpoints.Projects.Deployments.RedeployDeployment;

public sealed class RedeployDeploymentCommandHandler(
	IDeploymentRepository deploymentRepository,
	ISender sender) : ICommandHandler<RedeployDeploymentCommand, RedeployDeploymentResponse>
{
	public async Task<RedeployDeploymentResponse> Handle(RedeployDeploymentCommand command, CancellationToken cancellationToken = default)
	{
		var deployment = await deploymentRepository.GetByIdAsync(
			command.DeploymentId,
			cancellationToken) ??
			throw new InvalidOperationException($"Deployment with ID {command.DeploymentId} not found.");

		var deployProjectCommand = new DeployProjectCommand(
			deployment.ProjectId,
			deployment.CommitHash,
			deployment.CommitMessage,
			deployment.CommitAuthor,
			deployment.CommitBranch,
			deployment.CommitEmail,
			OverrideAuthorization: false);

		var result = await sender.Send(deployProjectCommand, cancellationToken);

		return new RedeployDeploymentResponse(
		    result.StatusUrl);
	}
}

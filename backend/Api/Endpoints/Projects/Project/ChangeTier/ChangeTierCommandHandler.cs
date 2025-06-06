using Api.Abstractions;
using Api.Data.Repositories;
using Core.MediatR;
using MediatR;

namespace Api.Endpoints.Projects.Project.ChangeTier;

public sealed class ChangeTierCommandHandler(
	IProjectRepository projectRepository,
	IDeploymentRepository deploymentRepository,
	IMediator mediator) : ICommandHandler<ChangeTierCommand, ChangeTierResponse>
{
	public async Task<ChangeTierResponse> Handle(ChangeTierCommand command, CancellationToken cancellationToken = default)
	{
		var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken) ??
			throw new InvalidOperationException($"Project with ID {command.ProjectId} not found.");

		await ChangeTierOnEntityAsync(project, command, cancellationToken);

		await RedeployLastDeploymentAsync(project, cancellationToken);

		return new ChangeTierResponse();
	}

	private async Task ChangeTierOnEntityAsync(
		Data.Project project,
		ChangeTierCommand command,
		CancellationToken cancellationToken)
	{
		project.ServerTierId = ServerTiers.GetById(new ServerTierId(command.Tier)).Id;

		await projectRepository.UpdateAsync(project, cancellationToken);
	}

	private async Task RedeployLastDeploymentAsync(
		Data.Project project,
		CancellationToken cancellationToken)
	{
		var currentDeploymentId = await deploymentRepository.GetCurrentDeploymentId(project.Id);

		if (currentDeploymentId is null)
		{
			Log.Error("No current deployment found for project {ProjectId}. No redeployment will occur.",
					project.Id);

			return; // No deployment to redeploy
		}

		var deployment = await deploymentRepository.GetByIdAsync(
			currentDeploymentId,
			cancellationToken);

		if (deployment is null)
		{
			Log.Error("Deployment with ID {DeploymentId} not found. No redeployment will occur.",
					currentDeploymentId);

			return; // Deployment not found
		}

		var newDeploymentCommand = new DeployProjectCommand(
			project.Id,
			deployment.CommitHash,
			deployment.CommitMessage,
			deployment.CommitAuthor,
			deployment.CommitBranch,
			deployment.CommitEmail);

		await mediator.Send(
		    newDeploymentCommand,
		    cancellationToken);
	}
}

using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Jobs;
using Api.Hosting.Services;
using Core.Extensions;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.DeploymentTrigger;

public sealed class DeploymentTriggerCommandHandler(
	IDeploymentRepository deploymentRepository,
	IProjectRepository projectRepository) : ICommandHandler<DeploymentTriggerCommand, DeploymentTriggerResponse>
{
	public async Task<DeploymentTriggerResponse> Handle(
		DeploymentTriggerCommand command,
		CancellationToken cancellationToken = default)
	{
		var deployments = await deploymentRepository.GetDeploymentsForProject(command.ProjectId)
											  .Where(d => d.Status == Data.DeploymentStatus.Queued || d.Status == Data.DeploymentStatus.Deploying)
											  .OrderByDescending(d => d.CreatedAtUtc)
											  .ToListAsync(cancellationToken);

		if (deployments.Count == 0)
		{
			Log.ForContext(nameof(command), command, true)
			   .Information("No queued or deploying deployments found");
			return new();
		}

		if (deployments.Any(x => x.Status == Data.DeploymentStatus.Deploying))
		{
			Log.ForContext(nameof(command), command, true)
			   .Information("Another deployment is already deploying");
			return new();
		}

		var allQueuedDeployments = deployments.Where(x => x.Status == Data.DeploymentStatus.Queued).ToList();

		if (allQueuedDeployments.Count > 1)
		{
			Log.ForContext(nameof(command), command, true)
			   .Information("Multiple queued deployments found, only the latest will be processed and all other will be cancelled");

			var deploymentsThatNeedCancellation = deployments.Where(x => x.Status == Data.DeploymentStatus.Queued).Skip(1);
			deploymentsThatNeedCancellation.Apply(d => d.Status = Data.DeploymentStatus.Cancelled);

			await deploymentRepository.UpdateRangeAsync(deploymentsThatNeedCancellation, cancellationToken);
		}

		var latestQueuedDeployment = allQueuedDeployments.First();

		DeployProjectJob.Enqueue(latestQueuedDeployment.Id);

		await HandleFirstDeploymentAsync(
			latestQueuedDeployment.Id,
			cancellationToken);

		return new();
	}

	private async Task HandleFirstDeploymentAsync(DeploymentId enqueuedDeploymentId, CancellationToken cancellationToken)
	{
		var deployment = await deploymentRepository.GetByIdAsync(enqueuedDeploymentId, cancellationToken);

		if (deployment is null)
		{
			Log.ForContext(nameof(enqueuedDeploymentId), enqueuedDeploymentId, true)
			   .Error("Deployment not found for the given ID. This should not happen as the deployment should be queued before this command is executed.");
			return;
		}

		var oldestDeployment = await deploymentRepository.GetDeploymentsForProject(deployment.ProjectId)
			.OrderBy(d => d.CreatedAtUtc)
			.FirstOrDefaultAsync(cancellationToken);

		if (oldestDeployment is not null && oldestDeployment.Id == deployment.Id)
		{
			try
			{
				await projectRepository.SetProjectStateAsync(deployment.ProjectId, ProjectState.Starting);
			}
			catch { }
		}
	}
}

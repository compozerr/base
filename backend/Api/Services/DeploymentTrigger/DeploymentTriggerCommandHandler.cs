using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data.Repositories;
using Api.Hosting.Jobs;
using Api.Hosting.Services;
using Core.Extensions;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.DeploymentTrigger;

public sealed class DeploymentTriggerCommandHandler(
	IDeploymentRepository deploymentRepository) : ICommandHandler<DeploymentTriggerCommand, DeploymentTriggerResponse>
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
			   .Information("Already deploying");
			return new();
		}

		if (deployments.Where(x => x.Status == Data.DeploymentStatus.Queued).Count() > 1)
		{
			Log.ForContext(nameof(command), command, true)
			   .Information("Multiple queued deployments found, only the latest will be processed and all other will be cancelled");

			var deploymentsThatNeedCancellation = deployments.Where(x => x.Status == Data.DeploymentStatus.Queued).Skip(1);
			deploymentsThatNeedCancellation.Apply(d => d.Status = Data.DeploymentStatus.Cancelled);

			await deploymentRepository.UpdateRangeAsync(deploymentsThatNeedCancellation, cancellationToken);
		}

		var latestQueuedDeployment = deployments.First();

		DeployProjectJob.Enqueue(latestQueuedDeployment.Id);
		return new();
	}
}

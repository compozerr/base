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
		var queuedDeployments = await deploymentRepository.GetDeploymentsForProject(command.ProjectId)
											  .Where(d => d.Status == Data.DeploymentStatus.Queued)
											  .OrderByDescending(d => d.CreatedAtUtc)
											  .ToListAsync(cancellationToken);

		if (queuedDeployments.Count == 0)
		{
			Log.ForContext(nameof(command), command, true)
			   .Information("No queued or deploying deployments found");
			return new();
		}
		
		if (queuedDeployments.Count > 1)
		{
			Log.ForContext(nameof(command), command, true)
			   .Information("Multiple queued deployments found, only the latest will be processed and all other will be cancelled");

			var deploymentsThatNeedCancellation = queuedDeployments.Skip(1);
			deploymentsThatNeedCancellation.Apply(d => d.Status = Data.DeploymentStatus.Cancelled);

			await deploymentRepository.UpdateRangeAsync(deploymentsThatNeedCancellation, cancellationToken);
		}

		var latestQueuedDeployment = queuedDeployments.First();

		DeployProjectJob.Enqueue(latestQueuedDeployment.Id);
		return new();
	}
}

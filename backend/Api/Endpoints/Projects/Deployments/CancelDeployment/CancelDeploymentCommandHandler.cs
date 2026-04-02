using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Deployments.Logs.Add;
using Core.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Deployments.CancelDeployment;

public sealed class CancelDeploymentCommandHandler(
	IDeploymentRepository deploymentRepository,
	IMediator mediator) : ICommandHandler<CancelDeploymentCommand, CancelDeploymentResponse>
{
	public async Task<CancelDeploymentResponse> Handle(
		CancelDeploymentCommand command,
		CancellationToken cancellationToken = default)
	{
		var deployment = await deploymentRepository.GetByIdAsync(
			command.DeploymentId,
			q => q.Include(d => d.Project),
			cancellationToken);

		if (deployment is null || deployment.ProjectId != command.ProjectId)
			return new CancelDeploymentResponse(false);

		if (deployment.Status is not (DeploymentStatus.Queued or DeploymentStatus.Deploying))
			return new CancelDeploymentResponse(false);

		deployment.Status = DeploymentStatus.Cancelled;
		deployment.BuildDuration = deployment.StartedAtUtc.HasValue
			? DateTime.UtcNow - deployment.StartedAtUtc.Value
			: null;
		await deploymentRepository.UpdateAsync(deployment, cancellationToken);

		await mediator.Send(new AddLogCommand(
			deployment.Id,
			"Deployment cancelled by user",
			Api.Abstractions.LogLevel.Warning), cancellationToken);

		await mediator.Send(
			new DeploymentTriggerCommand(deployment.ProjectId),
			cancellationToken);

		return new CancelDeploymentResponse(true);
	}
}

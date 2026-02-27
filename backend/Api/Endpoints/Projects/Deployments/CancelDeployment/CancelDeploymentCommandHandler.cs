using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Deployments.Logs.Add;
using Api.Hosting.Services;
using Core.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Deployments.CancelDeployment;

public sealed class CancelDeploymentCommandHandler(
	IDeploymentRepository deploymentRepository,
	IHostingApiFactory hostingApiFactory,
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

		if (deployment.Status == DeploymentStatus.Deploying)
		{
			try
			{
				var api = await hostingApiFactory.GetHostingApiAsync(
					deployment.Project?.ServerId ?? throw new Exception("No server assigned"));
				await api.CancelDeploymentAsync(deployment.Id);
			}
			catch (Exception ex)
			{
				Log.ForContext("DeploymentId", deployment.Id.Value)
				   .Error(ex, "Failed to send cancel to hosting manager, marking as cancelled locally");
			}
		}

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

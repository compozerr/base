using System.Data;
using Api.Data;
using Api.Data.Repositories;
using FluentValidation;

namespace Api.Endpoints.Projects.Deployments.RedeployDeployment;

public sealed class RedeployDeploymentCommandValidator : AbstractValidator<RedeployDeploymentCommand>
{
	public RedeployDeploymentCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
		var deploymentRepository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();

		RuleFor(x => x.DeploymentId)
			.MustAsync(async (deploymentId, cancellationToken) =>
			{
				var deployment = await deploymentRepository.GetByIdAsync(deploymentId, cancellationToken);
				return deployment != null;
			})
			.WithMessage("Deployment does not exist.");
		
		RuleFor(x => x.DeploymentId).MustAsync(async (deploymentId, cancellationToken) =>
		{
			var deploymentRepository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
			var projectId = (await deploymentRepository.GetByIdAsync(deploymentId, cancellationToken))?.ProjectId;
			var deploymentsInProgress = await deploymentRepository.GetFilteredAsync(x => x.ProjectId == projectId && (x.Status == DeploymentStatus.Deploying || x.Status == DeploymentStatus.Queued), cancellationToken);

			return deploymentsInProgress.Count == 0;
		}).WithMessage("You already have a deployment in progress");
	}
}

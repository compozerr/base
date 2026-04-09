using System.Data;
using Api.Data;
using Api.Data.Repositories;
using FluentValidation;

namespace Api.Endpoints.Projects.Deployments.RedeployDeployment;

public sealed class RedeployDeploymentCommandValidator : AbstractValidator<RedeployDeploymentCommand>
{
	public RedeployDeploymentCommandValidator(IServiceScopeFactory scopeFactory)
	{
		RuleFor(x => x.DeploymentId)
			.MustAsync(async (deploymentId, cancellationToken) =>
			{
				await using var scope = scopeFactory.CreateAsyncScope();
				var deploymentRepository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();

				var deployment = await deploymentRepository.GetByIdAsync(deploymentId, cancellationToken);
				return deployment != null;
			})
			.WithMessage("Deployment does not exist.");

		RuleFor(x => x.DeploymentId).MustAsync(async (deploymentId, cancellationToken) =>
		{
			await using var scope = scopeFactory.CreateAsyncScope();
			var deploymentRepository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();

			var deployment = await deploymentRepository.GetByIdAsync(deploymentId, cancellationToken);
			var deploymentsInProgress = await deploymentRepository.GetFilteredAsync(
				x => x.ProjectId == deployment!.ProjectId
				     && x.EnvironmentId == deployment.EnvironmentId
				     && (x.Status == DeploymentStatus.Deploying || x.Status == DeploymentStatus.Queued),
				cancellationToken);

			return deploymentsInProgress.Count == 0;
		}).WithMessage("You already have a deployment in progress for this environment");
	}
}

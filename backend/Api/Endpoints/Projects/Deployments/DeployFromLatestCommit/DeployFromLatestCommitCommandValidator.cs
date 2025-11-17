using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using FluentValidation;

namespace Api.Endpoints.Projects.Deployments.DeployFromLatestCommit;

public sealed class DeployFromLatestCommitCommandValidator : AbstractValidator<DeployFromLatestCommitCommand>
{
	public DeployFromLatestCommitCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();

		RuleFor(x => x.ProjectId)
			.MustBeOwnedByCallerAsync(scopeFactory);

		RuleFor(x => x.ProjectId).MustAsync(async (projectId, cancellationToken) =>
		{
			var deploymentRepository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();
			var deploymentsInProgress = await deploymentRepository.GetFilteredAsync(x => x.ProjectId == projectId && (x.Status == DeploymentStatus.Deploying || x.Status == DeploymentStatus.Queued), cancellationToken);

			return deploymentsInProgress.Count == 0;
		}).WithMessage("You already have a deployment in progress");
	}
}

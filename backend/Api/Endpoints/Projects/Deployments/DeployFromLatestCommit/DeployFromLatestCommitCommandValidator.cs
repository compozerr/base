using Api.Abstractions;
using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using FluentValidation;

namespace Api.Endpoints.Projects.Deployments.DeployFromLatestCommit;

public sealed class DeployFromLatestCommitCommandValidator : AbstractValidator<DeployFromLatestCommitCommand>
{
	public DeployFromLatestCommitCommandValidator(
	    IServiceScopeFactory scopeFactory)
	{
		RuleFor(x => x.ProjectId)
			.MustBeOwnedByCallerAsync(scopeFactory);

		RuleFor(x => x).MustAsync(async (command, cancellationToken) =>
		{
			await using var scope = scopeFactory.CreateAsyncScope();
			var deploymentRepository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();

			var deploymentsInProgress = await deploymentRepository.GetFilteredAsync(
			    x => x.ProjectId == command.ProjectId
			         && (command.EnvironmentId == null || x.EnvironmentId == command.EnvironmentId)
			         && (x.Status == DeploymentStatus.Deploying || x.Status == DeploymentStatus.Queued),
			    cancellationToken);

			return deploymentsInProgress.Count == 0;
		}).WithMessage("You already have a deployment in progress for this environment");
	}
}

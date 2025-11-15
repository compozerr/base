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
	}
}

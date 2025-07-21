using Api.Data.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Endpoints.Projects.ProjectEnvironment.ChangeAutoDeploy;

public sealed class ChangeAutoDeployCommandValidator : AbstractValidator<ChangeAutoDeployCommand>
{
	public ChangeAutoDeployCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
		RuleFor(command => command.AutoDeploy)
			.NotNull()
			.WithMessage("AutoDeploy cannot be null.")
			.Must(autoDeploy => autoDeploy == true || autoDeploy == false)
			.WithMessage("AutoDeploy must be a boolean value (true or false).");

		RuleFor(command => command.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
	}
}

using Api.Abstractions;
using FluentValidation;

namespace Api.Services.DeploymentTrigger;

public sealed class DeploymentTriggerCommandValidator : AbstractValidator<DeploymentTriggerCommand>
{
	public DeploymentTriggerCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
		// Add required services using scope.ServiceProvider.GetRequiredService<T>()

		// Add validation rules using RuleFor()
	}
}

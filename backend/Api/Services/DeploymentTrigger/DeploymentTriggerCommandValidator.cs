using Api.Abstractions;
using FluentValidation;

namespace Api.Services.DeploymentTrigger;

public sealed class DeploymentTriggerCommandValidator : AbstractValidator<DeploymentTriggerCommand>
{
	public DeploymentTriggerCommandValidator()
	{
		// Add validation rules using RuleFor()
	}
}

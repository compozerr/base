using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Endpoints.Modules.ForkModule;

public sealed class ForkModuleCommandValidator : AbstractValidator<ForkModuleCommand>
{
	public ForkModuleCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
		// Add required services using scope.ServiceProvider.GetRequiredService<T>()

		// Add validation rules using RuleFor()
	}
}

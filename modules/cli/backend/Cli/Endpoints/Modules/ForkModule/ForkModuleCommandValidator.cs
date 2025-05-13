using Api.Data.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Endpoints.Modules.ForkModule;

public sealed class ForkModuleCommandValidator : AbstractValidator<ForkModuleCommand>
{
	public ForkModuleCommandValidator(IServiceScopeFactory scopeFactory)
	{
		RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
	}
}

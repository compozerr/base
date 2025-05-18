using Api.Data.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Endpoints.Projects.RestoreProject;

public sealed class RestoreProjectCommandValidator : AbstractValidator<RestoreProjectCommand>
{
	public RestoreProjectCommandValidator(IServiceScopeFactory scopeFactory)
	{
		RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
	}
}

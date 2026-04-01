using Api.Data.Extensions;
using Api.Validation;
using FluentValidation;

namespace Api.Endpoints.Projects.Project.ChangeTier;

public sealed class ChangeTierCommandValidator : AbstractValidator<ChangeTierCommand>
{
	public ChangeTierCommandValidator(IServiceScopeFactory scopeFactory)
	{
		RuleFor(x => x.Tier).MustBeValidServerTier();

		RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
	}
}
